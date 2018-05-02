using System;
using System.Collections.Generic;
using System.Text;

namespace JJA.Anperi.Ipc.Dto
{
    public enum IpcMessageCode
    {
        Unset = 0,
        /// <summary>
        /// The default implementation on the server should parse the message and send it back.
        /// </summary>
        Debug = 1,
        Error = 2,
        GetPeripheralInfo = 3,
        SetPeripheralLayout = 4,
        SetPeripheralElementParam = 5,
        PeripheralEventFired = 6
    }

    public class IpcMessage
    {
        public IpcMessageCode MessageCode { get; set; } = IpcMessageCode.Unset;
        public Dictionary<string, dynamic> Data { get; set; } = new Dictionary<string, dynamic>();
    }
}
