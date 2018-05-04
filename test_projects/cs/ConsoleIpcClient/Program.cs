using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JJA.Anperi.Ipc.Client;
using JJA.Anperi.Ipc.Client.NamedPipe;
using JJA.Anperi.Ipc.Dto;
using Newtonsoft.Json;

namespace ConsoleIpcClient
{
    class Program
    {
        private static volatile bool _cancelRead = false;
        static void Main(string[] args)
        {
            Trace.Listeners.Add(new ConsoleTraceListener());
            Console.WriteLine("Connecting to endpoint ...");
            using (IIpcClient client = new NamedPipeIpcClient())
            {
                client.Opened += Client_Opened;
                client.Closed += Client_Closed;
                client.Error += Client_Error;
                client.MessageReceived += Client_MessageReceived;
                client.Connect();
                bool stop = false;
                while (!stop)
                {
                    Console.WriteLine("Enter text to send it to the server (or 'exit' to close): ");
                    string s = Console.ReadLine();
                    if ("exit".Equals(s) || _cancelRead) stop = true;
                    else
                    {
                        client.Send(new IpcMessage { MessageCode = IpcMessageCode.Debug, Data = new Dictionary<string, dynamic> {{"msg", s}}});
                    }
                }
                Console.WriteLine("Exiting ...");
                client.Disconnect();
            }
        }

        private static void Client_MessageReceived(object sender, JJA.Anperi.Ipc.Dto.IpcMessageEventArgs e)
        {
            Trace.TraceInformation("Received msg from server: " + JsonConvert.SerializeObject(e.Message));
        }

        private static void Client_Error(object sender, System.IO.ErrorEventArgs e)
        {
            Trace.TraceError("Error in client: {0}", e.GetException().Message);
            _cancelRead = true;
        }

        private static void Client_Closed(object sender, EventArgs e)
        {
            Trace.TraceWarning("Client closed.");
            _cancelRead = true;
        }

        private static void Client_Opened(object sender, EventArgs e)
        {
            Trace.TraceInformation("Client opened.");
        }
    }
}
