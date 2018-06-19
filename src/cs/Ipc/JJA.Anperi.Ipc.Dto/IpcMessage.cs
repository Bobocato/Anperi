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
        
        SetPeripheralLayout = 101,
        SetPeripheralElementParam = 102,
        ClaimControl = 103,
        FreeControl = 104,

        PeripheralEventFired = 200,
        PeripheralDisconnected = 201,
        PeripheralConnected = 202,
        ControlLost = 203,
        NotClaimed = 204
    }

    public class IpcMessage
    {
        public IpcMessage()
        {
        }
        public IpcMessage(IpcMessageCode code)
        {
            MessageCode = code;
        }

        public IpcMessageCode MessageCode { get; set; }
        public Dictionary<string, dynamic> Data { get; set; }
    }
}
