using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JJA.Anperi.Ipc.Dto;

namespace JJA.Anperi.Ipc.Client
{
    public interface IIpcClient : IDisposable
    {
        void Connect();
        void Disconnect();

        void Send(IpcMessage message);

        bool IsOpen { get; }

        event EventHandler<IpcMessageEventArgs> MessageReceived;
        event EventHandler<ErrorEventArgs> Error;
        event EventHandler Closed;
        event EventHandler Opened;
    }
}
