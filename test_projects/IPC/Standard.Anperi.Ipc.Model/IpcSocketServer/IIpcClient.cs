using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IpcSocketServer
{
    public interface IIpcClient
    {
        Task Send(string message);
        string Id { get; }
        event EventHandler<MessageEventArgs> Message;
    }
}
