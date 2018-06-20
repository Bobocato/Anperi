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

        /// <summary>
        /// The API version this library got compiled for.
        /// </summary>
        public int ApiVersion => 1;

        /// <summary>
        /// Here will arrive all events that aren't strictly bound to the status of the library.
        /// </summary>
        public event EventHandler<AnperiMessageEventArgs> Message;
        /// <summary>
        /// Occurs when the library gets connected to the IPC server. <seealso cref="IsConnected"/>
        /// </summary>
        public event EventHandler Connected;
        /// <summary>
        /// Occurs when the library gets disconnected to the IPC server. <seealso cref="IsConnected"/>
        /// </summary>
        public event EventHandler Disconnected;
        /// <summary>
        /// Occurs when a peripheral connected that has an incompatible version. 
        /// You can display this to the user or just use it (beware of sudden nuclear meltdowns).
        /// </summary>
        public event EventHandler<PeripheralConnectedEventArgs> IncompatibleDeviceConnected;
        /// <summary>
        /// A compatible peripheral connected and is ready to use. This doesn't imply that you have control!
        /// </summary>
        public event EventHandler<PeripheralConnectedEventArgs> PeripheralConnected;
        /// <summary>
        /// Occurs when a peripheral disconnects. <seealso cref="IncompatibleDeviceConnected"/> <seealso cref="PeripheralConnected"/>
        /// </summary>
        public event EventHandler PeripheralDisconnected;
        /// <summary>
        /// Send by the IPC server when no client library has control over it at the current time.
        /// </summary>
        public event EventHandler HostNotClaimed;
        /// <summary>
        /// Somebody claimed control over the peripheral and you lost control.
        /// !!!DO NOT RETAKE CONTROL HERE BECAUSE THIS COULD LEAD TO A PERMANENT LOOP!!!
        /// </summary>
        public event EventHandler ControlLost;
        
        /// <summary>
        /// Defines screen orientations known to anperi.
        /// </summary>
        public enum ScreenOrientation
        {
            unspecified, landscape, portrait
        }

        /// <summary>
        /// If the library is connected to the IPC server.
        /// </summary>
        public bool IsConnected => _ipcClient?.IsOpen ?? false;
        /// <summary>
        /// If you have control at the moment. This doesn't imply that a device is connected at the moment <seealso cref="IsPeripheralConnected"/>
        /// </summary>
        public bool HasControl { get; private set; } = false;
        /// <summary>
        /// If a peripheral is connected at the moment.
        /// </summary>
        public bool IsPeripheralConnected { get; private set; } = false;

        /// <summary>
        /// Combines <see cref="HasControl"/> and <see cref="IsPeripheralConnected"/> into one check.
        /// </summary>
        public bool CanControl => HasControl && IsPeripheralConnected;

        /// <summary>
        /// Claim control. After this (obviously awaited) you can control a connected device. 
        /// </summary>
        public async Task ClaimControl()
        {
            await _ipcClient.SendAsync(new IpcMessage(IpcMessageCode.ClaimControl)).ConfigureAwait(false);
            HasControl = true;
        }

        private void ThrowIfCantControl()
        {
            if (!HasControl) throw new InvalidOperationException("We aren't in control of the device.");
            if (!IsPeripheralConnected) throw new InvalidOperationException("There is no device to control.");
        }

        /// <summary>
        /// Indicate that you don't need control at the moment to make room for some background application. You can always get control back with <see cref="ClaimControl"/>.
        /// This won't trigger a <see cref="ControlLost"/> event. If you need that use <see cref="FreeControl(bool)"/>.
        /// </summary>
        public async Task FreeControl()
        {
            await FreeControl(false).ConfigureAwait(false);
        }

        /// <summary>
        /// Indicate that you don't need control at the moment to make room for some background application. You can always get control back with <see cref="ClaimControl"/>.
        /// </summary>
        /// <param name="fireControlLost">If the method call should trigger <see cref="ControlLost"/>. Wil only trigger if <see cref="HasControl"/> was true before.</param>
        public async Task FreeControl(bool fireControlLost)
        {
            if (HasControl)
            {
                await _ipcClient.SendAsync(new IpcMessage(IpcMessageCode.FreeControl)).ConfigureAwait(false);
                if (fireControlLost) OnControlLost();
            }
        }

        /// <summary>
        /// The info about the currently connected peripheral, will be null if no peripheral is connected.
        /// </summary>
        public PeripheralInfo PeripheralInfo { get; private set; }

        /// <summary>
        /// Set the layout in the connected peripheral.
        /// </summary>
        /// <param name="layout">the layout to be displayed</param>
        /// <param name="orientation">The screen orientation to be used. This should always be set in order to avoid flipping screens when picking up/laying down a device.</param>
        /// <exception cref="InvalidOperationException">If you don't have control <see cref="HasControl"/> or there is no peripheral connected <see cref="IsPeripheralConnected"/>.</exception>
        public async Task SetLayout(RootGrid layout, ScreenOrientation orientation = ScreenOrientation.unspecified)
        {
            ThrowIfCantControl();
            await _ipcClient.SendAsync(new IpcMessage {MessageCode = IpcMessageCode.SetPeripheralLayout, Data = new Dictionary<string, dynamic>{{"grid", layout}, {"orientation", orientation.ToString()}}}).ConfigureAwait(false);
        }

        /// <summary>
        /// Updates a parameter of an element that is already displayed on the peripheral.
        /// Not that you cannot change the layout parameters after the fact. You'll need to recreate a layout for that.
        /// </summary>
        /// <param name="elementId">the id you gave the element in <see cref="SetLayout"/></param>
        /// <param name="paramName">the name of the propery you want to change (same name as the properties in the element classes)</param>
        /// <param name="value">the value you want to give to the property</param>
        /// <exception cref="InvalidOperationException">If you don't have control <see cref="HasControl"/> or there is no peripheral connected <see cref="IsPeripheralConnected"/>.</exception>
        public async Task UpdateElementParam(string elementId, string paramName, dynamic value)
        {
            ThrowIfCantControl();
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
}
