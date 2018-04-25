using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IpcSocketServer
{
    class NamedPipeServer
    {
        public static string PipeName { get; } = "5573a4cb-03d2-47a2-b4e7-5db71c3fcd91";
        private CancellationTokenSource _cts = new CancellationTokenSource();

        private int _maxConnections = 100;
        public void Run()
        {
            for (int i = 0; i < _maxConnections; i++)
            {
                HandleConnection(i.ToString(), _cts.Token);
            }
        }

        private async void HandleConnection(string id, CancellationToken cancellationToken)
        {
            NamedPipeServerStream pipeServer = new NamedPipeServerStream("someUniqueName123", PipeDirection.InOut, _maxConnections);
            Trace.TraceInformation($"Server {id} waiting for connection ...");
            await pipeServer.WaitForConnectionAsync(cancellationToken);
            Trace.TraceInformation($"Client connected on id: {id}");
            try
            {
                StreamString ss = new StreamString(pipeServer);
                while (pipeServer.IsConnected)
                {
                    ss.WriteString("This is an awesome message");
                }
                string result = ss.ReadString();
                Trace.TraceInformation($"Conn {id} got: {result}");
            }
            catch (IOException e)
            {
                Trace.TraceError(e.Message);
            }
        }
    }
}
