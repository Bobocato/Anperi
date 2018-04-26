using System;

namespace JJA.Anperi.Ipc.Dto
{
    public class IpcMessageEventArgs : EventArgs
    {
        public IpcMessageEventArgs(IpcMessage message)
        {
            Message = message;
        }

        public IpcMessage Message { get; set; }
    }
}
