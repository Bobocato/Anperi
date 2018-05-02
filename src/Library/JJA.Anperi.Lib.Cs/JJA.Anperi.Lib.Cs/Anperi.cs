using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using JJA.Anperi.Ipc.Client;
using JJA.Anperi.Ipc.Client.NamedPipe;
using JJA.Anperi.Ipc.Dto;
using JJA.Anperi.Lib.Cs.Message;
using JJA.Anperi.Utility;

namespace JJA.Anperi.Lib.Cs
{
    public class Anperi
    {
        public event EventHandler<AnperiMessageEventArgs> DebugMessage;
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
            _ipcClient.Connect();
        }

        public bool IsConnected => _ipcClient.IsOpen;

        private void _ipcClient_Error(object sender, System.IO.ErrorEventArgs e)
        {
            Util.TraceException("IIpcClient encountered error", e.GetException());
            OnDisconnected();
            Task.Run(async () =>
            {
                Trace.TraceInformation("Reconnecting in 1 second ...");
                await Task.Delay(1000);
                Trace.TraceInformation("Reconnecting now ...");
                if (!_ipcClient.IsOpen) _ipcClient.Connect();
            });
        }

        private void _ipcClient_Closed(object sender, EventArgs e)
        {
            Trace.TraceWarning("IIpcClient closed.");
            OnDisconnected();
            Task.Run(async () =>
            {
                Trace.TraceInformation("Reconnecting in 1 second ...");
                await Task.Delay(1000);
                Trace.TraceInformation("Reconnecting now ...");
                if (!_ipcClient.IsOpen) _ipcClient.Connect();
            });
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
                    OnMessage(new PeripheralInfoMessage(e.Message.Data));
                    break;
                case IpcMessageCode.PeripheralEventFired:
                    OnMessage(new EventFiredAnperiMessage(e.Message.Data));
                    break;
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
            DebugMessage?.Invoke(this, new AnperiMessageEventArgs(e));
        }

        protected virtual void OnConnected()
        {
            Connected?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnDisconnected()
        {
            Disconnected?.Invoke(this, EventArgs.Empty);
        }
    }
}
