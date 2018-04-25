using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IpcSocketServer
{
    public interface IIpcClient
    {
        void StartReceive();
        void Close();
        void Send(string message);
        string Id { get; }
        event EventHandler<MessageEventArgs> Message;
        event EventHandler Closed;
    }
}
