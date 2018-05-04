using System.Collections.Generic;
using JJA.Anperi.Ipc.Dto;
using JJA.Anperi.Utility;

namespace JJA.Anperi.Lib.Message
{
    public class DebugAnperiMessage : AnperiMessage
    {
        protected override IpcMessageCode MsgCode => IpcMessageCode.Debug;

        public string Message
        {
            get
            {
                base.Data.TryGetValue("msg", out string msg);
                return msg;
            }
            set => base.Data["msg"] = value;
        }

        public DebugAnperiMessage(Dictionary<string, dynamic> data) : base(data)
        {
        }

        public DebugAnperiMessage(string message)
        {
            Message = message;
        }
    }
}
