using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JJA.Anperi.Ipc.Dto;

namespace JJA.Anperi.Ipc.Client.NamedPipe
{
    public class NamedPipeIpcClient : IIpcClient
    {
        public void Connect()
        {
            throw new NotImplementedException();
        }

        public void Disconnect()
        {
            throw new NotImplementedException();
        }

        public void Send(IpcMessage message)
        {
            throw new NotImplementedException();
        }

        public bool IsOpen
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public event EventHandler<IpcMessageEventArgs> MessageReceived;
        public event EventHandler<ErrorEventArgs> Error;
        public event EventHandler Closed;
        public event EventHandler Opened;

        public void Dispose()
        {
        }
    }
}
