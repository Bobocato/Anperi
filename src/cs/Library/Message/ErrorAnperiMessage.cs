using System.Collections.Generic;
using JJA.Anperi.Ipc.Dto;
using JJA.Anperi.Utility;

namespace JJA.Anperi.Lib.Message
{
    public class ErrorAnperiMessage : AnperiMessage
    {
        protected override IpcMessageCode MsgCode => IpcMessageCode.Error;

        public ErrorAnperiMessage(Dictionary<string, dynamic> data) : base(data)
        {
        }

        public ErrorAnperiMessage(string message)
        {
            Message = message;
        }

        public string Message
        {
            get
            {
                base.Data.TryGetValue("msg", out string msg);
                return msg;
            }
            set => base.Data["msg"] = value;
        }
    }
}
