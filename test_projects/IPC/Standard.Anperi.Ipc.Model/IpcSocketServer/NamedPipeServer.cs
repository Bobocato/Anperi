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
    public class NamedPipeServer
    {
        private static string PipeName { get; } = "anperi.lib.ipc.server";
        public static string InputPipeName => PipeName + ".input";
        public static string OutputPipeName => PipeName + ".output";

        public bool IsRunning
        {
            get { return _running; }
        }

        private readonly Dictionary<string, NamedPipeIpcClient> _clients = new Dictionary<string, NamedPipeIpcClient>();
        private readonly Dictionary<string, NamedPipeIpcClient> _pendingClients = new Dictionary<string, NamedPipeIpcClient>();
        private CancellationTokenSource _cts;
        private readonly SemaphoreSlim _semaphoreStartInputConnections = new SemaphoreSlim(5, 5);
        private readonly SemaphoreSlim _semaphoreStartOutputConnections = new SemaphoreSlim(5, 5);

        private readonly object _syncRootClients = new object();

        private bool _running = false;
        
        private int _maxConnections = 100;
        public void Run()
        {
            _running = true;
            _cts = new CancellationTokenSource();
            KeepEnoughConnections(_cts.Token);
        }

        public void Close()
        {
            _running = false;
            foreach (NamedPipeIpcClient namedPipeIpcClient in _clients.Values)
            {
                namedPipeIpcClient.Close();
            }
            _cts?.Cancel();
        }

        private async void KeepEnoughConnections(CancellationToken token)
        {
            await Task.Run(async () =>
            {
                int c = 0;
                while (!token.IsCancellationRequested)
                {
                    if (_semaphoreStartInputConnections.CurrentCount > 0)
                    {
                        CreatePipe((c++).ToString(), token);
                    }

                    try { await Task.Delay(TimeSpan.FromMilliseconds(100), token); }
                    catch
                    { 
                        // ignored 
                    }
                }
            }, token);
        }

        private void CreatePipe(string id, CancellationToken cancellationToken)
        {
            CreateInputPipe("i_" + id, cancellationToken);
            CreateOutputPipe("o_" + id, cancellationToken);
        }

        private async void CreateInputPipe(string id, CancellationToken cancellationToken)
        {
            
            NamedPipeServerStream pipeServer = new NamedPipeServerStream(InputPipeName, PipeDirection.InOut, _maxConnections);
            Trace.TraceInformation($"Server {id} waiting for connection ...");
            await _semaphoreStartInputConnections.WaitAsync(cancellationToken);
            try
            {
                await pipeServer.WaitForConnectionAsync(cancellationToken);
            }
            catch (Exception e)
            {
                Trace.TraceError("Error waiting for pipe connection: {0}", e.Message);
            }
            finally { _semaphoreStartInputConnections.Release(); }
            Trace.TraceInformation($"Client connected on id: {id}");
            try
            {
                StreamString ss = new StreamString(pipeServer, null);
                string clientId = await ss.ReadString(cancellationToken);
                lock (_syncRootClients)
                {
                    if (_pendingClients.TryGetValue(clientId, out NamedPipeIpcClient val))
                    {
                        if (val.InputPipe == null)
                        {
                            val.InputPipe = pipeServer;
                            _clients.Add(val.Id, val);
                            _pendingClients.Remove(val.Id);
                        }
                        else
                        {
                            Trace.TraceWarning("Client tried to register two input pipes.");
                            pipeServer.Close();
                        }
                    }
                    else
                    {
                        _pendingClients.Add(clientId, new NamedPipeIpcClient(clientId) { InputPipe = pipeServer });
                    }
                }
            }
            catch (IOException e)
            {
                Trace.TraceError(e.Message);
            }
        }

        private async void CreateOutputPipe(string id, CancellationToken cancellationToken)
        {

            using (NamedPipeServerStream pipeServer =
                new NamedPipeServerStream(OutputPipeName, PipeDirection.InOut, _maxConnections))
            {
                Trace.TraceInformation($"Server {id} waiting for connection ...");
                await _semaphoreStartOutputConnections.WaitAsync(cancellationToken);
                try
                {
                    await pipeServer.WaitForConnectionAsync(cancellationToken);
                }
                catch (Exception e)
                {
                    Trace.TraceError("Error waiting for pipe connection: {0}", e.Message);
                }
                finally { _semaphoreStartOutputConnections.Release(); }
                Trace.TraceInformation($"Client connected on id: {id}");
                try
                {
                    StreamString ss = new StreamString(pipeServer, pipeServer);
                    string clientId = await ss.ReadString(cancellationToken);
                    lock (_syncRootClients)
                    {
                        if (_pendingClients.TryGetValue(clientId, out NamedPipeIpcClient val))
                        {
                            if (val.OutputPipe == null)
                            {
                                val.OutputPipe = pipeServer;
                                _clients.Add(val.Id, val);
                                _pendingClients.Remove(val.Id);
                            }
                            else
                            {
                                Trace.TraceWarning("Client tried to register two input pipes.");
                                pipeServer.Close();
                            }
                        }
                        else
                        {
                            _clients.Add(clientId, new NamedPipeIpcClient(clientId) {OutputPipe = pipeServer});
                        }
                    }
                }
                catch (IOException e)
                {
                    Trace.TraceError(e.Message);
                }
                catch (ArgumentException)
                {
                    Trace.TraceInformation("Client ? disconnected.");
                }
            }
        }
    }
}
