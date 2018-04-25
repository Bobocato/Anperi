using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IpcSocketServer
{
    public interface IIpcServer
    {
        void Run();
        void Stop();
        bool IsRunning { get; }

        event EventHandler<ClientEventArgs> ClientConnected;
        event EventHandler<ClientEventArgs> ClientDisconnected;

        event EventHandler<ErrorEventArgs> Error;
        event EventHandler Closed;

    }
}
