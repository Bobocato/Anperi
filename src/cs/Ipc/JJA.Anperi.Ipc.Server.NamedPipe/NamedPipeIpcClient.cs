using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using JJA.Anperi.Ipc.Common.NamedPipe;
using JJA.Anperi.Ipc.Dto;
using JJA.Anperi.Utility;
using Newtonsoft.Json;

namespace JJA.Anperi.Ipc.Server.NamedPipe
{
    internal class NamedPipeIpcClient : IIpcClient
    {
        internal NamedPipeServerStream InputPipe
        {
            get { return _inputPipe; }
            set
            {
                if (_inputPipe != null) throw new ArgumentException("Can't set InputPipe twice.");
                _inputPipe = value;
                if (OutputPipe != null && value != null)
                {
                    _streamString = new StreamString(InputPipe, OutputPipe);
                }
            }
        }

        internal NamedPipeServerStream OutputPipe
        {
            get { return _outputPipe; }
            set
            {
                if (_outputPipe != null) throw new ArgumentException("Can't set OutputPipe twice.");
                _outputPipe = value;
                if (InputPipe != null && value != null)
                {
                    _streamString = new StreamString(InputPipe, OutputPipe);
                }
            }
        }

        public bool IsAlive
        {
            get { return InputPipe.IsConnected && OutputPipe.IsConnected && !_threwException; }
        }

        private bool _threwException = false;
        private bool _isReceiving = false;
        private StreamString _streamString = null;
        private NamedPipeServerStream _inputPipe;
        private NamedPipeServerStream _outputPipe;


        internal NamedPipeIpcClient(string id)
        {
            Id = id;
        }

        public void StartReceive()
        {
            StartReceive(CancellationToken.None);
        }

        public void Close()
        {
            _threwException = true;
            InputPipe.Close();
            OutputPipe.Close();
        }

        public void StartReceive(CancellationToken cancellationToken)
        {
            if (_isReceiving) throw new InvalidOperationException("Startreceive can only be called once.");
            _isReceiving = true;
            _streamString.WriteString(Settings.ConnectionSuccessString);
            ReceiveLoopAsync(cancellationToken); 
        }

        public void Send(IpcMessage message)
        {
            try
            {
                _streamString?.WriteString(JsonConvert.SerializeObject(message));
            }
            catch (ArgumentException)
            {
                _threwException = true;
                OnClosed();
            }
            catch (Exception ex)
            {
                Util.TraceException("Error receiving data from pipe.", ex);
                _threwException = true;
                OnClosed();
            }
        }

        private async void ReceiveLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested && IsAlive)
                {
                    string s = await _streamString.ReadString(token);
                    OnMessage(s);
                }
            }
            catch (ArgumentException)
            {
                _threwException = true;
                OnClosed();
            }
            catch (Exception ex)
            {
                Util.TraceException("Error receiving data from pipe.", ex);
                _threwException = true;
                OnClosed();
            }
        }

        public string Id { get; }

        public event EventHandler<IpcMessageEventArgs> Message;

        protected void OnMessage(string message)
        {
            Trace.TraceInformation($"Received: {message}");
            IpcMessage im = null;
            try
            {
                im = JsonConvert.DeserializeObject<IpcMessage>(message);
            }
            catch (JsonException ex)
            {
                Util.TraceException("Error parsing JSON:", ex);
            }
            if (im != null) Message?.Invoke(this, new IpcMessageEventArgs(im));
        }

        public event EventHandler Closed;

        protected void OnClosed()
        {
            Closed?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            _inputPipe?.Dispose();
            _outputPipe?.Dispose();
        }
    }
}