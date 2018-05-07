using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JJA.Anperi.Ipc.Dto;

namespace JJA.Anperi.Ipc.Client
{
    public interface IIpcClient : IDisposable
    {
        Task ConnectAsync();
        Task ConnectAsync(CancellationToken ct);
        void Disconnect();
        void Send(IpcMessage message);
        Task SendAsync(IpcMessage message);
        Task SendAsync(IpcMessage message, CancellationToken ct);
        bool IsOpen { get; }

        event EventHandler<IpcMessageEventArgs> MessageReceived;
        event EventHandler<ErrorEventArgs> Error;
        event EventHandler Closed;
        event EventHandler Opened;
    }
}
