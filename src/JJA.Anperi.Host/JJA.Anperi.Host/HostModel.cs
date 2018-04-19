using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JJA.Anperi.HostApi;

namespace JJA.Anperi.Host
{
    class HostModel
    {
        public HostModel()
        {
            Peripherals = new List<HostJsonApiObjectFactory.ApiPeripheral>();
        }

        public string Info1 { get; set; } = "No Current WebSocket connection!";
        public string Info2 { get; set; } = "";
        public string Info3 { get; set; } = "";
        public string ConnectedTo { get; set; } = "Connected to:";
        public List<HostJsonApiObjectFactory.ApiPeripheral> Peripherals { get; set; }
        public bool ButConnectVisible { get; set; } = true;
        public bool ButDisconnectVisible { get; set; } = false;

        public string WebSocketAddress { get; set; } = "wss://anperi.jannes-peters.com/api/ws";
        //public string WebSocketAddress { get; set; } = "ws://localhost:5000/api/ws";

        public string Token { get; set; } = "";
    }
}
