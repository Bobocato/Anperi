using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JJA.Anperi.Ipc.Common.NamedPipe;
using JJA.Anperi.Utility;

namespace JJA.Anperi.Ipc.Server.NamedPipe
{
    public class NamedPipeIpcServer : IIpcServer
    {

        public bool IsRunning
        {
            get { return _running; }
        }

        public event EventHandler<ClientEventArgs> ClientConnected;
        public event EventHandler<ClientEventArgs> ClientDisconnected;
        public event EventHandler<ErrorEventArgs> Error;
        public event EventHandler Closed;

        private readonly Dictionary<string, NamedPipeIpcClient> _clients = new Dictionary<string, NamedPipeIpcClient>();
        private readonly Dictionary<string, NamedPipeIpcClient> _pendingClients = new Dictionary<string, NamedPipeIpcClient>();
        private CancellationTokenSource _cts;
        private readonly SemaphoreSlim _semaphoreStartInputConnections = new SemaphoreSlim(5, 5);
        private readonly SemaphoreSlim _semaphoreStartOutputConnections = new SemaphoreSlim(5, 5);

        private readonly object _syncRootClients = new object();

        private bool _running = false;
        private int _maxConnections = 100;

        public void Start()
        {
            if (_running) throw new InvalidOperationException("The server is already started.");
            _running = true;
            _cts = new CancellationTokenSource();
            KeepEnoughConnections(_cts.Token);
        }

        public void Stop()
        {
            _running = false;
            _cts?.Cancel();
            _cts?.Dispose();
            lock (_syncRootClients)
            {
                foreach (NamedPipeIpcClient namedPipeIpcClient in _clients.Values)
                {
                    try
                    {
                        namedPipeIpcClient.Close();
                        namedPipeIpcClient.Dispose();
                    }
                    catch (Exception)
                    {
                        //ignored
                    }
                }
                _clients.Clear();
            }
            OnClosed();
        }

        public List<IIpcClient> Clients
        {
            get
            {
                lock (_syncRootClients)
                {
                    return _clients.Values.Cast<IIpcClient>().ToList();
                }
            }
        }

        private async void KeepEnoughConnections(CancellationToken token)
        {
            await Task.Run(async () =>
            {
                int iId = 0;
                int oId = 0;
                while (!token.IsCancellationRequested)
                {
                    if (_semaphoreStartInputConnections.CurrentCount > 0)
                    {
                        CreatePipe((iId++).ToString(), PipeDirection.In, token);
                    }
                    if (_semaphoreStartOutputConnections.CurrentCount > 0)
                    {
                        CreatePipe((oId++).ToString(), PipeDirection.Out, token);
                    }
                    try { await Task.Delay(TimeSpan.FromMilliseconds(100), token); }
                    catch
                    {
                        // ignored 
                    }
                }
            }, token);
        }

        private async void CreatePipe(string id, PipeDirection direction, CancellationToken token)
        {
            try
            {
                if (direction == PipeDirection.InOut)
                    throw new ArgumentException("InOut is not a valid value. Use In OR Out!");
                id = direction == PipeDirection.In ? $"i_{id}" : $"o_{id}";
                NamedPipeServerStream pipeServer = new NamedPipeServerStream(
                    direction == PipeDirection.In ? Settings.ServerInputPipeName : Settings.ServerOutputPipeName,
                    PipeDirection.InOut, _maxConnections);
                Trace.TraceInformation($"Server {id} waiting for connection ...");
                SemaphoreSlim semaphore = direction == PipeDirection.In
                    ? _semaphoreStartInputConnections
                    : _semaphoreStartOutputConnections;
                await semaphore.WaitAsync(token);
                try
                {
                    await pipeServer.WaitForConnectionAsync(token);
                }
                catch (Exception e)
                {
                    Trace.TraceError("Error waiting for pipe connection: {0}", e.Message);
                    pipeServer.Dispose();
                    return;
                }
                finally
                {
                    semaphore.Release();
                }
                Trace.TraceInformation($"Client connected on id: {id}");
                try
                {
                    StreamString ss = new StreamString(pipeServer, null);
                    string clientId = await ss.ReadString(token);
                    lock (_syncRootClients)
                    {
                        if (_pendingClients.TryGetValue(clientId, out NamedPipeIpcClient val))
                        {
                            if (direction == PipeDirection.In)
                            {
                                if (val.InputPipe == null) val.InputPipe = pipeServer;
                                else
                                    Trace.TraceWarning(
                                        $"Client {val.Id} tried to register two input pipes, closing ...");
                            }
                            else
                            {
                                if (val.OutputPipe == null) val.OutputPipe = pipeServer;
                                else
                                    Trace.TraceWarning(
                                        $"Client {val.Id} tried to register two output pipes, closing ...");
                            }
                            if (val.InputPipe != null && val.OutputPipe != null)
                            {
                                Trace.TraceInformation($"Client {val.Id} successfully connected two pipes.");
                                _clients.Add(val.Id, val);
                                _pendingClients.Remove(val.Id);
                                OnClientConnected(val);
                            }
                            else
                            {
                                pipeServer.Close();
                                pipeServer.Dispose();
                            }
                        }
                        else
                        {
                            Trace.TraceInformation($"Client {clientId} registered on {id}");
                            NamedPipeIpcClient npic = new NamedPipeIpcClient(clientId);
                            if (direction == PipeDirection.In) npic.InputPipe = pipeServer;
                            else npic.OutputPipe = pipeServer;
                            npic.Closed += NamedPipeIpcClient_Closed;
                            _pendingClients.Add(clientId, npic);
                        }
                    }
                }
                catch (IOException e)
                {
                    Trace.TraceError(e.Message);
                }
                catch (ArgumentException)
                {
                    Trace.TraceInformation($"Client disconnected before he sent an Id on connection {id}.");
                    pipeServer.Dispose();
                }
            }
            catch (Exception ex)
            {
                Util.TraceException("Error creating pipe", ex);
                OnError(ex);
            }
        }

        private void NamedPipeIpcClient_Closed(object sender, EventArgs e)
        {
            NamedPipeIpcClient s = (NamedPipeIpcClient)sender;
            lock (_syncRootClients)
            {
                if (_clients.ContainsKey(s.Id))
                {
                    OnClientClosed(s);
                    _clients.Remove(s.Id);
                }
                else
                {
                    _pendingClients.Remove(s.Id);
                }
            }
            s.Dispose();
        }

        private void OnError(Exception e)
        {
            Error?.Invoke(this, new ErrorEventArgs(e));
            Stop();
        }

        private void OnClosed()
        {
            Closed?.Invoke(this, EventArgs.Empty);
        }

        private void OnClientClosed(NamedPipeIpcClient val)
        {
            ClientDisconnected?.Invoke(this, new ClientEventArgs(val));
        }

        private void OnClientConnected(NamedPipeIpcClient val)
        {
            ClientConnected?.Invoke(this, new ClientEventArgs(val));
        }

        public void Dispose()
        {
            foreach (NamedPipeIpcClient client in _pendingClients.Values)
            {
                try { client?.Dispose(); } catch (ObjectDisposedException) { }
            }
            foreach (NamedPipeIpcClient client in _clients.Values)
            {
                try { client?.Dispose(); } catch (ObjectDisposedException) { }
            }
            try { _cts?.Dispose(); } catch(ObjectDisposedException) { }
            try { _semaphoreStartInputConnections?.Dispose(); } catch (ObjectDisposedException) { }
            try { _semaphoreStartOutputConnections?.Dispose(); } catch (ObjectDisposedException) { }
        }
    }
}
