using System;
using System.Collections.Generic;
using System.Text;
using JJA.Anperi.Ipc.Dto;

namespace JJA.Anperi.Lib.Message
{
    public class PeripheralConnectedAnperiMessage : AnperiMessage
    {
        protected override IpcMessageCode MsgCode => IpcMessageCode.PeripheralConnected;

        public PeripheralConnectedAnperiMessage(Dictionary<string, dynamic> data) : base(data)
        {
            PeripheralInfo = new PeripheralInfo(data);
        }

        public PeripheralInfo PeripheralInfo { get; private set; }
    }
}
