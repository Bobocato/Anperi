using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace JJA.Anperi.Ipc.Server
{
    public interface IIpcServer : IDisposable
    {
        void Start();
        void Stop();
        bool IsRunning { get; }

        event EventHandler<ClientEventArgs> ClientConnected;
        event EventHandler<ClientEventArgs> ClientDisconnected;

        event EventHandler<ErrorEventArgs> Error;
        event EventHandler Closed;
    }
}
