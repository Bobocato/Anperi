using System;
using System.Collections.Generic;
using System.Text;
using JJA.Anperi.Ipc.Dto;

namespace JJA.Anperi.Ipc.Server
{
    public interface IIpcClient : IDisposable
    {
        void StartReceive();
        void Close();
        void Send(IpcMessage message);

        string Id { get; }

        event EventHandler<IpcMessageEventArgs> Message;
        event EventHandler Closed;
    }
}
