using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using JJA.Anperi.Ipc.Dto;

namespace JJA.Anperi.Ipc.Server
{
    public interface IIpcClient : IDisposable
    {
        void StartReceive();
        void Close();
        Task SendAsync(IpcMessage message);

        string Id { get; }

        event EventHandler<IpcMessageEventArgs> Message;
        event EventHandler Closed;
    }
}
