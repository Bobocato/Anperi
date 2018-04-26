using System;
using System.Collections.Generic;
using System.Text;

namespace JJA.Anperi.Ipc.Server
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
