using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IpcSocketServer
{
    //TODO: implement IDisposable for NamedPopeServerStream
    public class NamedPipeIpcClient : IIpcClient
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
        private StreamString _streamString = null;
        private NamedPipeServerStream _inputPipe;
        private NamedPipeServerStream _outputPipe;


        public NamedPipeIpcClient(string id)
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
            ReceiveLoop(cancellationToken);
            _streamString.WriteString("OK");
        }

        public void Send(string message)
        {
            _streamString?.WriteString(message);
        }

        private async void ReceiveLoop(CancellationToken token)
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
            }
        }

        public string Id { get; }

        public event EventHandler<MessageEventArgs> Message;
        protected void OnMessage(string message)
        {
            Message?.Invoke(this, new MessageEventArgs(message));
        }

        public event EventHandler Closed;
        protected void OnClosed()
        {
            Closed?.Invoke(this, EventArgs.Empty);
        }

    }
}
