using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IpcSocketServer
{
    public class ClientEventArgs : EventArgs
    {
        public ClientEventArgs(IIpcClient client)
        {
            Client = client;
        }
        public IIpcClient Client { get; set; }
    }

    public class MessageEventArgs : EventArgs
    {
        public MessageEventArgs(string message)
        {
            Message = message;
        }
        public string Message { get; set; }
    }
}
