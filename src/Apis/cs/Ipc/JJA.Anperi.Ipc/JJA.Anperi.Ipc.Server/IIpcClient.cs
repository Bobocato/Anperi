using System;
using System.Collections.Generic;
using System.Text;

namespace JJA.Anperi.Ipc.Server
{
    public interface IIpcClient : IDisposable
    {
        void StartReceive();
        void Close();
        void Send(string message);
        string Id { get; }
        event EventHandler<MessageEventArgs> Message;
        event EventHandler Closed;
    }
}
