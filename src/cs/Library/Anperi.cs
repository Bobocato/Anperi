using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using JJA.Anperi.Ipc.Client;
using JJA.Anperi.Ipc.Client.NamedPipe;
using JJA.Anperi.Ipc.Dto;
using JJA.Anperi.Lib.Elements;
using JJA.Anperi.Lib.Message;
using JJA.Anperi.Utility;

namespace JJA.Anperi.Lib
{
    public class Anperi
    {
        public event EventHandler<AnperiMessageEventArgs> Message;
        public event EventHandler Connected;
        public event EventHandler Disconnected; 

        private readonly IIpcClient _ipcClient;

        public Anperi()
        {
            _ipcClient = new NamedPipeIpcClient();
            _ipcClient.Opened += _ipcClient_Opened;
            _ipcClient.MessageReceived += _ipcClient_MessageReceived;
            _ipcClient.Closed += _ipcClient_Closed;
            _ipcClient.Error += _ipcClient_Error;
            _ipcClient.ConnectAsync();
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

        public async Task RequestPeripheralInfo()
        {
            if (!HasControl) throw new InvalidOperationException("We aren't in control of the device.");
            await _ipcClient.SendAsync(new IpcMessage {MessageCode = IpcMessageCode.GetPeripheralInfo}).ConfigureAwait(false);
        }

        public async Task SetLayout(RootGrid layout)
        {
            if (!HasControl) throw new InvalidOperationException("We aren't in control of the device.");
            await _ipcClient.SendAsync(new IpcMessage {MessageCode = IpcMessageCode.SetPeripheralLayout, Data = new Dictionary<string, dynamic>{{"grid", layout}}}).ConfigureAwait(false);
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

        private async void _ipcClient_Error(object sender, System.IO.ErrorEventArgs e)
        {
            Util.TraceException("IIpcClient encountered error", e.GetException());
            OnDisconnected();
            await Task.Run(async () =>
            {
                Trace.TraceInformation("Reconnecting in 1 second ...");
                await Task.Delay(1000);
                Trace.TraceInformation("Reconnecting now ...");
                if (!_ipcClient.IsOpen) await _ipcClient.ConnectAsync().ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        private async void _ipcClient_Closed(object sender, EventArgs e)
        {
            Trace.TraceWarning("IIpcClient closed.");
            OnDisconnected();
            await Task.Run(async () =>
            {
                Trace.TraceInformation("Reconnecting in 1 second ...");
                await Task.Delay(1000);
                Trace.TraceInformation("Reconnecting now ...");
                if (!_ipcClient.IsOpen) await _ipcClient.ConnectAsync();
            }).ConfigureAwait(false);
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
                case IpcMessageCode.GetPeripheralInfo:
                    OnMessage(new PeripheralInfoAnperiMessage(e.Message.Data));
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
                    OnPeripheralConnected();
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
        public event EventHandler HostNotClaimed;

        protected virtual void OnControlLost()
        {
            ControlLost?.Invoke(this, EventArgs.Empty);
        }
        public event EventHandler ControlLost;

        protected virtual void OnPeripheralConnected()
        {
            PeripheralConnected?.Invoke(this, EventArgs.Empty);
        }
        public event EventHandler PeripheralConnected;

        protected virtual void OnPeripheralDisconnected()
        {
            PeripheralDisconnected?.Invoke(this, EventArgs.Empty);
        }
        public event EventHandler PeripheralDisconnected;
    }
}
