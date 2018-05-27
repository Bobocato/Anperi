using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JJA.Anperi.Ipc.Common.NamedPipe;
using JJA.Anperi.Ipc.Dto;
using JJA.Anperi.Utility;
using Newtonsoft.Json;

namespace JJA.Anperi.Ipc.Client.NamedPipe
{
    public class NamedPipeIpcClient : IIpcClient
    {
        private NamedPipeClientStream _streamIn, _streamOut;
        private StreamString _ss;
        private CancellationTokenSource _cts;
        private string _token;
        private readonly SemaphoreSlim _stateLock;
        private State _state;

        private enum State
        {
            Disconnected, Connecting, Connected, Disconnecting
        }
        
        public NamedPipeIpcClient()
        {
            _state = State.Disconnected;
            _stateLock = new SemaphoreSlim(1, 1);
        }

        public Task ConnectAsync() => ConnectAsync(CancellationToken.None);

        public async Task ConnectAsync(CancellationToken ct)
        {
            await _stateLock.WaitAsync(ct);
            try
            {
                switch (_state)
                {
                    case State.Disconnected:
                        _state = State.Connecting;
                        break;
                    case State.Connecting:
                    case State.Connected:
                        return;
                    case State.Disconnecting:
                        throw new InvalidOperationException("Cannot connect while disconnecting.");
                }
            }
            finally
            {
                _stateLock.Release();
            }
            _cts = new CancellationTokenSource();
            try
            {
                _token = "cs.api.referencelib." + Guid.NewGuid();
                _streamIn = new NamedPipeClientStream(Settings.ServerOutputPipeName);
                _streamOut = new NamedPipeClientStream(Settings.ServerInputPipeName);

                await Task.WhenAll(
                    _streamIn.ConnectAsync(2000, ct),
                    _streamOut.ConnectAsync(2000, ct)
                ).ConfigureAwait(false);

                await Task.WhenAll(
                    StreamString.WriteStringAsync(_streamIn, _token, ct),
                    StreamString.WriteStringAsync(_streamOut, _token, ct)
                ).ConfigureAwait(false);

                _ss = new StreamString(_streamIn, _streamOut);
                string res = await _ss.ReadStringAsync(ct).ConfigureAwait(false);
                if (Settings.ConnectionSuccessString.Equals(res))
                {
                    await ThrowIfNotStateAndSetNewState(State.Connecting, State.Connected, ct);
                    OnOpened();
                    ReceiveLoop(_cts.Token);
                }
                else
                {
                    throw new AuthenticationException("Couldn't successfully authenticate with server.");
                }
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        }

        private async Task ThrowIfNotStateAndSetNewState(State expectedState, State newState, CancellationToken ct)
        {
            await _stateLock.WaitAsync(ct);
            try
            {
                if (_state != expectedState)
                    throw new InvalidOperationException($"The client is not {expectedState.ToString()}.");
                _state = newState;
            }
            finally
            {
                _stateLock.Release();
            }
        }

        private async void ReceiveLoop(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested && IsOpen)
                {
                    string s = await _ss.ReadStringAsync(token);
                    IpcMessage msg = null;
                    if (s == null) throw new Exception("Got null string from server.");
                    try
                    {
                        msg = JsonConvert.DeserializeObject<IpcMessage>(s, new DictionaryDeserializer());
                    }
                    catch (JsonException ex)
                    {
                        Util.TraceException("Error parsing incoming JSON", ex);
                    }
                    if (msg != null) OnMessageReceived(msg);
                }
            }
            catch (ArgumentException)
            {
                OnClosed();
            }
            catch (Exception ex)
            {
                Util.TraceException("Error receiving data from pipe.", ex);
                OnError(ex);
            }
        }

        public void Disconnect()
        {
            _stateLock.Wait();
            _state = State.Disconnecting;
            _stateLock.Release();
            try
            {
                _streamIn.Close();
                _streamOut.Close();
                _cts.Cancel();
            }
            catch (Exception ex)
            {
                Util.TraceException("Error disconnecting NamedPipeIpcClient", ex);
            }
            finally
            {
                OnClosed();
            }
        }

        public void Send(IpcMessage message)
        {
            if (!IsOpen || _ss == null) throw new InvalidOperationException("The client is not connected.");
            try
            {
                _ss.WriteString(JsonConvert.SerializeObject(message));
            }
            catch (ArgumentException)
            {
                OnClosed();
            }
            catch (Exception ex)
            {
                Util.TraceException("Error receiving data from pipe.", ex);
                OnClosed();
            }
        }

        public Task SendAsync(IpcMessage message) => SendAsync(message, CancellationToken.None);

        public async Task SendAsync(IpcMessage message, CancellationToken ct)
        {
            if (!IsOpen || _ss == null) throw new InvalidOperationException("The client is not connected.");
            try
            {
                await _ss.WriteStringAsync(JsonConvert.SerializeObject(message), ct);
            }
            catch (ArgumentException)
            {
                OnClosed();
            }
            catch (Exception ex)
            {
                Util.TraceException("Error receiving data from pipe.", ex);
                OnClosed();
            }
        }

        private void ResetClient()
        {
            _stateLock.Wait();
            _state = State.Disconnecting;
            _stateLock.Release();
            try
            {
                _streamIn?.Dispose();
            }
            catch (ObjectDisposedException) { }
            _streamIn = null;
            try
            {
                _streamOut?.Dispose();
            }
            catch (ObjectDisposedException) { }
            _streamOut?.Dispose();
            _streamOut = null;
            _ss = null;
            try
            {
                _cts?.Dispose();
            }
            catch (ObjectDisposedException) { }
            _stateLock.Wait();
            _state = State.Disconnected;
            _stateLock.Release();
        }

        public bool IsOpen
        {
            get
            {
                _stateLock.Wait();
                bool connected = _state == State.Connected;
                _stateLock.Release();
                return _streamIn?.IsConnected == true && _streamOut?.IsConnected == true && connected;
            }
        }

        private void OnMessageReceived(IpcMessage message)
        {
            MessageReceived?.Invoke(this, new IpcMessageEventArgs(message));
        }
        public event EventHandler<IpcMessageEventArgs> MessageReceived;

        private void OnError(Exception exception)
        {
            ResetClient();
            Error?.Invoke(this, new ErrorEventArgs(exception));
        }
        public event EventHandler<ErrorEventArgs> Error;

        private void OnClosed()
        {
            ResetClient();
            Closed?.Invoke(this, EventArgs.Empty);
        }
        public event EventHandler Closed;

        private void OnOpened()
        {
            Opened?.Invoke(this, EventArgs.Empty);
        }
        public event EventHandler Opened;

        public void Dispose()
        {
            ResetClient();
            _stateLock?.Dispose();
        }
    }
}
