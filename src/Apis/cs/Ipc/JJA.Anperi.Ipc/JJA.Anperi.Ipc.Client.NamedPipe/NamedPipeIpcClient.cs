using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Security.Authentication;
using System.Text;
using System.Threading;
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
        private readonly CancellationTokenSource _cts;
        private string _token;
        private bool _isAuthenticated = false;


        public NamedPipeIpcClient()
        {
            _cts = new CancellationTokenSource();
        }

        public async void Connect()
        {
            try
            {
                _token = "cs.api.referencelib." + Guid.NewGuid();
                _streamIn = new NamedPipeClientStream(Settings.ServerOutputPipeName);
                _streamOut = new NamedPipeClientStream(Settings.ServerInputPipeName);
                await _streamIn.ConnectAsync(_cts.Token);
                await _streamOut.ConnectAsync(_cts.Token);

                StreamString.WriteString(_streamIn, _token);
                StreamString.WriteString(_streamOut, _token);
                
                _ss = new StreamString(_streamIn, _streamOut);
                string res = await _ss.ReadString(_cts.Token);
                if (Settings.ConnectionSuccessString.Equals(res))
                {
                    _isAuthenticated = true;
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
                _isAuthenticated = false;
                Util.TraceException("Error connecting to NamedPipe", ex);
                OnError(ex);
            }
        }

        private async void ReceiveLoop(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested && IsOpen)
                {
                    string s = await _ss.ReadString(token);
                    IpcMessage msg = null;
                    if (s == null) throw new Exception("Got null string from server.");
                    try
                    {
                        msg = JsonConvert.DeserializeObject<IpcMessage>(s);
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
            try
            {
                _isAuthenticated = false;
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
            if (!IsOpen) throw new InvalidOperationException("The client is not connected.");
            try
            {
                _ss?.WriteString(JsonConvert.SerializeObject(message));
            }
            catch (ArgumentException)
            {
                _isAuthenticated = false;
                OnClosed();
            }
            catch (Exception ex)
            {
                Util.TraceException("Error receiving data from pipe.", ex);
                _isAuthenticated = false;
                OnClosed();
            }
        }

        public bool IsOpen => _streamIn?.IsConnected == true && _streamOut?.IsConnected == true && _isAuthenticated;

        private void OnMessageReceived(IpcMessage message)
        {
            MessageReceived?.Invoke(this, new IpcMessageEventArgs(message));
        }
        public event EventHandler<IpcMessageEventArgs> MessageReceived;

        private void OnError(Exception exception)
        {
            Error?.Invoke(this, new ErrorEventArgs(exception));
            OnClosed();
        }
        public event EventHandler<ErrorEventArgs> Error;

        private void OnClosed()
        {
            _isAuthenticated = false;
            _streamIn?.Dispose();
            _streamIn = null;
            _streamOut?.Dispose();
            _streamOut = null;
            _ss = null;
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
            _isAuthenticated = false;
            _streamIn?.Dispose();
            _streamIn = null;
            _streamOut?.Dispose();
            _streamOut = null;
            _ss = null;
            _cts?.Dispose();
        }
    }
}
