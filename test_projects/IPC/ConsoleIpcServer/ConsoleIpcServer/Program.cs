using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using JJA.Anperi.Ipc.Dto;
using JJA.Anperi.Ipc.Server;
using JJA.Anperi.Ipc.Server.NamedPipe;

namespace ConsoleIpcServer
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            Trace.Listeners.Add(new ConsoleTraceListener());
            using (IIpcServer server = new NamedPipeIpcServer())
            {
                server.ClientConnected += Server_ClientConnected;
                server.ClientDisconnected += Server_ClientDisconnected;
                server.Closed += Server_Closed;
                server.Error += Server_Error;
                server.Start();

                Console.WriteLine("Started server ... hit enter to shutdown.");
                Console.ReadLine();

                server.Stop();
            }
        }

        private static void Server_Error(object sender, System.IO.ErrorEventArgs e)
        {
            Trace.TraceError($"Server errored: {e.GetException().Message}");
        }

        private static void Server_Closed(object sender, EventArgs e)
        {
            Trace.TraceInformation("Server closed.");
        }

        private static void Server_ClientDisconnected(object sender, ClientEventArgs e)
        {
            Trace.TraceInformation($"Client {e.Client.Id} disconnected.");
        }

        private static void Server_ClientConnected(object sender, ClientEventArgs e)
        {
            Trace.TraceInformation($"Client {e.Client.Id} connected.");
            e.Client.StartReceive();
            e.Client.Message += (o, args) => (o as IIpcClient)?.Send(args.Message);
        }
    }
}
