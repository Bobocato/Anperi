using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using JJA.Anperi.Ipc.Client;
using JJA.Anperi.Ipc.Client.NamedPipe;
using JJA.Anperi.Ipc.Dto;
using JJA.Anperi.Lib.Elements;
using JJA.Anperi.Lib.Message;
using JJA.Anperi.Utility;

[assembly: ComVisible(true)]
namespace JJA.Anperi.Lib
{
    /// <summary>
    /// Class for access to all the anperi functionality. After instantiation set the event listeners and call connect.
    /// Make sure to Dispose it to gracefully disconnect from the IPC server.
    /// </summary>
    public class Anperi : IDisposable
    {
        public int ApiVersion => 1;

        /// <summary>
        /// 
        /// </summary>
        public event EventHandler<AnperiMessageEventArgs> Message;
        public event EventHandler Connected;
        public event EventHandler Disconnected;
        public event EventHandler<PeripheralConnectedEventArgs> IncompatibleDeviceConnected;
        public event EventHandler<PeripheralConnectedEventArgs> PeripheralConnected;
        public event EventHandler PeripheralDisconnected;
        public event EventHandler HostNotClaimed;
        public event EventHandler ControlLost;

        private readonly IIpcClient _ipcClient;
        private readonly SemaphoreSlim _semConnecting = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Creates a new Anperi interface. It will connect to the IPC server automatically. To actually use this class you'll need to subscribe to almost all events.
        /// </summary>
        public Anperi()
        {
            _ipcClient = new NamedPipeIpcClient();
            _ipcClient.Opened += _ipcClient_Opened;
            _ipcClient.MessageReceived += _ipcClient_MessageReceived;
            _ipcClient.Closed += _ipcClient_Closed;
            _ipcClient.Error += _ipcClient_Error;
            ReconnectIn(TimeSpan.Zero);
        }

        public bool IsConnected => _ipcClient?.IsOpen ?? false;
        public bool HasControl { get; private set; } = false;
        public bool IsPeripheralConnected { get; private set; } = false;

        public async Task ClaimControl()
        {
            await _ipcClient.SendAsync(new IpcMessage(IpcMessageCode.ClaimControl)).ConfigureAwait(false);
            HasControl = true;
        }

        public async Task FreeControl()
        {
            if (!HasControl) throw new InvalidOperationException("We aren't in control of the device.");
            await _ipcClient.SendAsync(new IpcMessage(IpcMessageCode.FreeControl)).ConfigureAwait(false);
        }

        public PeripheralInfo PeripheralInfo { get; private set; }

        public async Task SetLayout(RootGrid layout, ScreenOrientation orientation = ScreenOrientation.unspecified)
        {
            if (!HasControl) throw new InvalidOperationException("We aren't in control of the device.");
            await _ipcClient.SendAsync(new IpcMessage {MessageCode = IpcMessageCode.SetPeripheralLayout, Data = new Dictionary<string, dynamic>{{"grid", layout}, {"orientation", orientation.ToString()}}}).ConfigureAwait(false);
        }

        public async Task UpdateElementParam(string elementId, string paramName, dynamic value)
        {
            if (!HasControl) throw new InvalidOperationException("We aren't in control of the device.");
            await _ipcClient.SendAsync(new IpcMessage(IpcMessageCode.SetPeripheralElementParam)
            {
                Data = new Dictionary<string, dynamic>
                {
                    {"param_name", paramName},
                    {"param_value", value},
                    {"id", elementId}
                }
            });
        }

        private void _ipcClient_Error(object sender, System.IO.ErrorEventArgs e)
        {
            //Util.TraceException("IIpcClient encountered error", e.GetException());
            OnDisconnected();
            ReconnectIn(TimeSpan.FromSeconds(1));
        }

        private void _ipcClient_Closed(object sender, EventArgs e)
        {
            Trace.TraceWarning("IIpcClient closed.");
            OnDisconnected();
            ReconnectIn(TimeSpan.FromSeconds(1));
        }

        private async void ReconnectIn(TimeSpan delay)
        {
            if (!await _semConnecting.WaitAsync(0)) return;
            try
            {
                Trace.TraceInformation("Reconnecting in {0} seconds ...", ((int)delay.TotalSeconds).ToString());
                await Task.Delay(delay);
                Trace.TraceInformation("Reconnecting now ...");
            }
            finally
            {
                _semConnecting.Release();
            }
            try
            {
                await _ipcClient.ConnectAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                //ignored
                //will be handled in _ipcClient_Error
            }
        }

        private void _ipcClient_MessageReceived(object sender, IpcMessageEventArgs e)
        {
            switch (e.Message.MessageCode)
            {
                case IpcMessageCode.Unset:
                    Trace.TraceError("Received IpcMessage with unset code.\n{0}", e.Message.Data.ToDataString());
                    break;
                case IpcMessageCode.Debug:
                    OnMessage(new DebugAnperiMessage(e.Message.Data));
                    break;
                case IpcMessageCode.Error:
                    OnMessage(new ErrorAnperiMessage(e.Message.Data));
                    break;
                case IpcMessageCode.PeripheralEventFired:
                    OnMessage(new EventFiredAnperiMessage(e.Message.Data));
                    break;
                case IpcMessageCode.PeripheralDisconnected:
                    IsPeripheralConnected = false;
                    OnPeripheralDisconnected();
                    break;
                case IpcMessageCode.PeripheralConnected:
                    IsPeripheralConnected = true;
                    var pCon = new PeripheralConnectedAnperiMessage(e.Message.Data);
                    PeripheralInfo = pCon.PeripheralInfo;
                    if (pCon.PeripheralInfo.Version != ApiVersion)
                    {
                        OnIncompatibleDeviceConnected(pCon.PeripheralInfo);
                    }
                    else
                    {
                        OnPeripheralConnected(new PeripheralConnectedAnperiMessage(e.Message.Data).PeripheralInfo);
                    }
                    break;
                case IpcMessageCode.ControlLost:
                    HasControl = false;
                    OnControlLost();
                    break;
                case IpcMessageCode.NotClaimed:
                    OnHostNotClaimed();
                    break;
                case IpcMessageCode.ClaimControl:
                case IpcMessageCode.FreeControl:
                case IpcMessageCode.SetPeripheralLayout:
                case IpcMessageCode.SetPeripheralElementParam:
                default:
                    Trace.TraceError("Message {0} not implemented, data:\n{1}", e.Message.MessageCode.ToString(), e.Message.Data.ToDataString());
                    break;
            }
        }

        private void _ipcClient_Opened(object sender, EventArgs e)
        {
            Trace.TraceInformation("IIpcClient Opened.");
            OnConnected();
        }

        public void Debug(string message)
        {
            if (!_ipcClient.IsOpen) return;
            _ipcClient.Send(new DebugAnperiMessage(message).ToIpcMessage());
        }

        protected virtual void OnMessage(AnperiMessage e)
        {
            Message?.Invoke(this, new AnperiMessageEventArgs(e));
        }

        protected virtual void OnConnected()
        {
            Connected?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnDisconnected()
        {
            Disconnected?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnHostNotClaimed()
        {
            HostNotClaimed?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnControlLost()
        {
            ControlLost?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnPeripheralConnected(PeripheralInfo msg)
        {
            PeripheralConnected?.Invoke(this, new PeripheralConnectedEventArgs(msg));
        }

        protected virtual void OnPeripheralDisconnected()
        {
            PeripheralDisconnected?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnIncompatibleDeviceConnected(PeripheralInfo msg)
        {
            IncompatibleDeviceConnected?.Invoke(this, new PeripheralConnectedEventArgs(msg));
        }

        public void Dispose()
        {
            _ipcClient?.Dispose();
            _semConnecting?.Dispose();
        }
    }

    public enum ScreenOrientation
    {
        unspecified, landscape, portrait
    }
}
