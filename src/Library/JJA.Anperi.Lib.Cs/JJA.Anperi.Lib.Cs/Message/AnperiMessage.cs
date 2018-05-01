using System;
using System.Collections.Generic;
using System.Text;
using JJA.Anperi.Ipc.Dto;

namespace JJA.Anperi.Lib.Cs.Message
{
    public abstract class AnperiMessage
    {
        protected AnperiMessage(Dictionary<string, dynamic> data)
        {
            Data = data;
        }

        protected AnperiMessage()
        {
            Data = new Dictionary<string, dynamic>();
        }

        public Dictionary<string, dynamic> Data { get; protected set; }
        protected abstract IpcMessageCode MsgCode { get; }

        internal IpcMessage ToIpcMessage()
        {
            return new IpcMessage { Data = Data, MessageCode = MsgCode };
        }
    }
}
