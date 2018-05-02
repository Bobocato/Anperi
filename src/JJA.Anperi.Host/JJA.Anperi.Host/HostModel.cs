using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;
using JJA.Anperi.Host.Utility;
using JJA.Anperi.Internal.Api;
using JJA.Anperi.Internal.Api.Device;
using JJA.Anperi.Internal.Api.Host;
using JJA.Anperi.Internal.Api.Shared;
using JJA.Anperi.Ipc.Dto;
using JJA.Anperi.Ipc.Server;
using JJA.Anperi.Ipc.Server.NamedPipe;
using Newtonsoft.Json.Linq;


namespace JJA.Anperi.Host
{
    class HostModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _info1 = "No Current WebSocket connection!";
        private string _info2 = "";
        private string _info3 = "";
        private string _connectedTo = "";
        private string _filePath = "token.txt";
        private string _name = "";
        private bool _butConnectVisible = true;
        private bool _butDisconnectVisible = false;

        private string _wsAddress = "ws://localhost:5000/api/ws";
        //private string _wsAddress = "wss://anperi.jannes-peters.com/api/ws";
        private string _token = "";
        private WebSocket _ws;
        private List<HostJsonApiObjectFactory.ApiPeripheral> _periList;
        private readonly Queue<string> _messages;
        private bool _closing = false;
        private int _connectedPeripheral;
        private NamedPipeIpcServer _ipcServer;
        private readonly List<IIpcClient> _ipcClients;
        private IIpcClient _curIpcClient;

        public HostModel()
        {
            _periList = new List<HostJsonApiObjectFactory.ApiPeripheral>();
            _ipcClients = new List<IIpcClient>();
            _messages = new Queue<string>();
            if (File.Exists(_filePath))
            {
                _token = File.ReadLines(_filePath).First();
            }
            InitializeWebSocket();
            InitializeIpcServer();
        }

        #region Properties

        public string Info1
        {
            get { return _info1; }
            set
            {
                _info1 = value;
                OnPropertyChanged(nameof(Info1));
            }
        }
        public string Info2
        {
            get { return _info2; }
            set
            {
                _info2 = value;
                OnPropertyChanged(nameof(Info2));
            }
        }
        public string Info3
        {
            get { return _info3; }
            set
            {
                _info3 = value;
                OnPropertyChanged(nameof(Info3));
            }
        }
        public string ConnectedTo
        {
            get { return _connectedTo; }
            set
            {
                _connectedTo = value;
                OnPropertyChanged(nameof(ConnectedTo));
            }
        }

        public List<HostJsonApiObjectFactory.ApiPeripheral> Peripherals
        {
            get { return _periList; }
            set
            {
                var tmp = from x in value orderby x.online descending select x;
                _periList = tmp.ToList();
                OnPropertyChanged(nameof(Peripherals));
            }
        }

        public bool ButConnect
        {
            get { return _butConnectVisible; }
            set
            {
                _butConnectVisible = value;
                OnPropertyChanged(nameof(ButConnect));
            }
        }
        public bool ButDisconnect
        {
            get { return _butDisconnectVisible; }
            set
            {
                _butDisconnectVisible = value;
                OnPropertyChanged(nameof(ButDisconnect));
            }
        }

        #endregion

        private void InitializeIpcServer()
        {
            Task.Run(() =>
            {
                _ipcServer = new NamedPipeIpcServer();               
                _ipcServer.ClientConnected += (sender, args) =>
                {
                    var client = args.Client;
                    _ipcClients.Add(client);
                    _curIpcClient = client;
                    client.Message += (o, eventArgs) =>
                    {
                        var message = eventArgs.Message;
                        var messageType = message.MessageCode;

                        switch (messageType)
                        {
                            case IpcMessageCode.Debug:
                                if (message.Data.TryGetValue("msg", out string msg))
                                {
                                    QueueMessage(msg);
                                }
                                break;
                            case IpcMessageCode.Unset:
                                client.Send(message);
                                break;
                            case IpcMessageCode.GetPeripheralInfo:
                                var getInfo = new JsonApiObject(JsonApiContextTypes.device, JsonApiMessageTypes.request, "get_info", message.Data);
                                SendToWebsocket(getInfo.Serialize());
                                break;
                            case IpcMessageCode.SetPeripheralElementParam:
                                var setParam = new JsonApiObject(JsonApiContextTypes.device, JsonApiMessageTypes.message, "set_element_param", message.Data);
                                SendToWebsocket(setParam.Serialize());
                                break;
                            case IpcMessageCode.SetPeripheralLayout:
                                var setLayout = new JsonApiObject(JsonApiContextTypes.device, JsonApiMessageTypes.message, "set_layout", message.Data);
                                SendToWebsocket(setLayout.Serialize());
                                break;
                            default:
                                throw new NotImplementedException($"Didnt implement: {messageType}");
                        }
                    };
                    client.StartReceive();
                };

                _ipcServer.ClientDisconnected += (sender, args) =>
                {
                    var client = args.Client;
                    _ipcClients.Remove(client);
                    if (_curIpcClient.Equals(client))
                    {
                        _curIpcClient = null;
                    }
                };

                _ipcServer.Closed += (sender, args) =>
                {
                    _ipcClients.ForEach(x => x.Close());
                    _ipcClients.Clear();
                    if (!_closing)
                    {
                        Thread.Sleep(2000);
                        QueueMessage("Ipc Server is restarting!");
                        _ipcServer.Start();
                    }
                };

                _ipcServer.Error += (sender, args) =>
                {
                    //TODO: maybe dc all clients here
                    Trace.TraceError("IPC-Server error: " + args.ToString());
                };

                _ipcServer.Start();
                QueueMessage("Ipc-Server is running!");
            });
        }

        private void InitializeWebSocket()
        {
            Task.Run(() =>
            {
                using (_ws = new WebSocket(_wsAddress))
                {
                    _ws.OnOpen += (sender, e) =>
                    {
                        if (_ws.IsAlive)
                        {
                            Info1 =
                                "Current WebSocket-Address is: " + _wsAddress;
                        }
                        if (_token.Equals(""))
                        {
                            var name = Environment.MachineName;
                            var json =
                                SharedJsonApiObjectFactory
                                    .CreateRegisterRequest(
                                        SharedJsonDeviceType.host, name);
                            _ws.Send(json.Serialize());
                        }
                        else
                        {
                            var json =
                                SharedJsonApiObjectFactory.CreateLoginRequest(
                                    _token, SharedJsonDeviceType.host);
                            string msg = json.Serialize();
                            _ws.Send(msg);
                        }
                    };

                    _ws.OnMessage += (sender, e) =>
                    {
                        Console.WriteLine(e.Data);
                        var json = JsonApiObject.Deserialize(e.Data);
                        var context = json.context;

                        switch (context)
                        {
                            case JsonApiContextTypes.server:
                                if (Enum.TryParse(json.message_code, out SharedJsonRequestCode type))
                                {
                                    HandleSharedRequestCode(type, json);
                                }
                                else if (Enum.TryParse(json.message_code, out HostRequestCode hostRequest))
                                {
                                    HandleHostRequestCode(hostRequest, json);
                                }
                                else if (Enum.TryParse(json.message_code, out SharedJsonMessageCode sharedMessage))
                                {
                                    HandleSharedMessageCode(sharedMessage, json);
                                }
                                else
                                {
                                    QueueMessage("Unknown server code!");
                                }
                                break;
                            case JsonApiContextTypes.device:
                                if (Enum.TryParse(json.message_code, out DeviceRequestCode deviceCode))
                                {
                                    HandleDeviceRequestCode(deviceCode, json);
                                }
                                else
                                {
                                    QueueMessage("Unknown device code!");
                                }
                                break;
                        }
                    };

                    _ws.OnClose += (sender, e) =>
                    {
                        if (!_closing)
                        {
                            Info1 = "No current WebSocket connection";
                            if (!_ws.IsAlive)
                            {
                                Thread.Sleep(2000);
                                _ws.Connect();
                            }
                        }
                    };

                    _ws.OnError += (sender, e) =>
                    {
                        Console.WriteLine("Error in WebSocket connection: " + e);
                    };

                    _ws.Connect();
                }
            });
        }

        #region response handling

        private void HandleHostRequestCode(HostRequestCode code,
    JsonApiObject json)
        {
            switch (code)
            {
                case HostRequestCode.pair:
                case HostRequestCode.unpair:
                    if (json.data.TryGetValue("success",
                        out dynamic pairSuccess))
                    {
                        if (pairSuccess)
                        {
                            SendPeripheralRequest();
                        }
                        else
                        {
                            QueueMessage(
                                "Something went wrong in operation " +
                                json.message_code + " !");
                        }
                    }
                    else
                    {
                        Trace.TraceWarning(
                            "success missed in pair/unpair response");
                    }

                    break;
                case HostRequestCode.get_available_peripherals:
                    json.data.TryGetValue("devices", out dynamic list);
                    if (list != null)
                    {
                        Peripherals.Clear();
                        foreach (var x in list)
                        {
                            JObject jo = x;
                            if (jo.TryGetCastValue("name", out string name) && jo.TryGetCastValue("id", out int id) && jo.TryGetCastValue("online", out bool online))
                            {
                                Peripherals.Add(new HostJsonApiObjectFactory.ApiPeripheral { id = id, name = name, online = online });
                            }
                            else
                            {
                                Trace.TraceError($"couldn't parse get_available_peripherals answer: {list.ToString()}");
                            }
                        }
                        OnPropertyChanged(nameof(Peripherals));
                    }
                    else
                    {
                        Trace.TraceWarning("Something is wrong with the send peripherals!");
                    }

                    break;
                case HostRequestCode.connect_to_peripheral:
                case HostRequestCode.disconnect_from_peripheral:
                    if (json.data.TryGetValue("success",
                        out dynamic connectSuccess))
                    {
                        if (connectSuccess)
                        {
                            if (json.message_code.Equals(
                                "connect_to_peripheral"))
                            {
                                if (json.data.TryGetValue("id", out int id))
                                {
                                    _connectedPeripheral = id;
                                }
                                var peripheral = Peripherals.SingleOrDefault(x => x.id == id);
                                if (peripheral != null)
                                {
                                    ConnectedTo = peripheral.name;
                                }
                                else
                                {
                                    QueueMessage("Couldn't find the connected device in peripheral list!");
                                }
                                ButConnect = false;
                                ButDisconnect = true;
                            }
                            else
                            {
                                ConnectedTo =
                                    "";
                                _connectedPeripheral = -1;
                                ButConnect = true;
                                ButDisconnect = false;
                            }
                        }
                        else
                        {
                            QueueMessage(
                                "Something went wrong in operation " +
                                json.message_code + " !");
                        }
                    }
                    else
                    {
                        Trace.TraceWarning(
                            "success missed in connect/disconnect response");
                    }

                    break;
            }
        }

        private void HandleSharedRequestCode(SharedJsonRequestCode code, JsonApiObject json)
        {
            switch (code)
            {
                case SharedJsonRequestCode.login:
                    if (json.data.TryGetValue("success",
                        out dynamic success))
                    {
                        if (success)
                        {
                            json.data.TryGetValue("name",
                                out dynamic loginName);
                            _name = loginName;
                            SendPeripheralRequest();
                            Info2 = "Your name is: " + _name;
                        }
                        else
                        {
                            _token = "";
                            QueueMessage("Login failed!");
                            var writer = new StreamWriter(_filePath, false);
                            writer.WriteLine("");
                            writer.Close();
                        }
                    }
                    else
                    {
                        Trace.TraceWarning(
                            "success missed in login response");
                    }

                    break;
                case SharedJsonRequestCode.register:
                    json.data.TryGetValue("token", out dynamic token);
                    _token = token;
                    json.data.TryGetValue("name", out dynamic name);
                    _name = name;

                    var tokenStream = new StreamWriter(_filePath, false);
                    tokenStream.WriteLine(_token);
                    tokenStream.Close();

                    SendPeripheralRequest();

                    Info2 = "Your name is: " + _name;
                    break;
                default:
                    throw new NotImplementedException(
                        $"Didnt implement: {code.ToString()}");
            }
        }

        private void HandleSharedMessageCode(SharedJsonMessageCode code, JsonApiObject json)
        {
            switch (code)
            {
                case SharedJsonMessageCode.error:
                    if (json.data.TryGetValue("message", out dynamic error))
                    {
                        Console.WriteLine(error);
                        QueueMessage(error);
                    }
                    break;
                case SharedJsonMessageCode.partner_disconnected:
                    ConnectedTo = "";
                    ButDisconnect = false;
                    ButConnect = true;
                    break;
            }
        }

        private void HandleDeviceRequestCode(DeviceRequestCode deviceCode, JsonApiObject json)
        {
            switch (deviceCode)
            {
                case DeviceRequestCode.debug:
                    if (json.data.TryGetValue("msg", out string msg))
                    {
                        QueueMessage(msg);
                    }
                    break;
                case DeviceRequestCode.get_info:
                    var getInfo = new IpcMessage
                    {
                        MessageCode = IpcMessageCode.GetPeripheralInfo,
                        Data = json.data                       
                    };
                    _curIpcClient.Send(getInfo);
                    break;
                case DeviceRequestCode.eventFired:
                    var eventFired = new IpcMessage
                    {
                        MessageCode = IpcMessageCode.PeripheralEventFired,
                        Data = json.data
                    };
                    _curIpcClient.Send(eventFired);
                    break;
                case DeviceRequestCode.error:
                    var errorMessage = new IpcMessage
                    {
                        MessageCode = IpcMessageCode.Error,
                        Data = json.data
                    };
                    _curIpcClient.Send(errorMessage);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(deviceCode), deviceCode, null);
            }
        }

        #endregion

        #region websocket requests

        private void SendPeripheralRequest()
        {
            var jsonPeri = HostJsonApiObjectFactory.CreateAvailablePeripheralRequest();
            SendToWebsocket(jsonPeri.Serialize());
        }

        public void Unpair(object item)
        {
            if (!(item is HostJsonApiObjectFactory.ApiPeripheral)) return;
            var id = ((HostJsonApiObjectFactory.ApiPeripheral)item).id;
            var json = HostJsonApiObjectFactory.CreateUnpairFromPeripheralRequest(id);
            SendToWebsocket(json.Serialize());
        }

        public void SendMessage(string message)
        {
            var json =
                DeviceJsonApiObjectFactory.CreateDebugRequest(message);
            SendToWebsocket(json.Serialize());
        }

        public void Connect(object item)
        {
            if (!(item is HostJsonApiObjectFactory.ApiPeripheral)) return;
            var id = ((HostJsonApiObjectFactory.ApiPeripheral)item).id;
            var json = HostJsonApiObjectFactory.CreateConnectToPeripheralRequest(id);
            SendToWebsocket(json.Serialize());
        }

        public void Disconnect()
        {
            var json = HostJsonApiObjectFactory.CreateDisconnectFromPeripheralRequest();
            SendToWebsocket(json.Serialize());
        }

        public void Pair(string pairCode)
        {
            var json =
                HostJsonApiObjectFactory.CreatePairingRequest(pairCode);
            SendToWebsocket(json.Serialize());
        }


        #endregion

        private void SendToWebsocket(string message)
        {
            Task.Run(() =>
            {
                if (_ws.IsAlive)
                {
                    _ws.Send(message);
                }
                else
                {
                    QueueMessage("Websocket is not alive!");
                }
            });
        }

        private void QueueMessage(string message)
        {
            _messages.Enqueue(message);
            if (Info3.Equals(""))
            {
                DequeueMessages();
            }
        }

        private void DequeueMessages()
        {
            Task.Run(() =>
            {
                if (_messages.Count != 0)
                {
                    Info3 = _messages.Dequeue();
                    Thread.Sleep(3000);
                    if (_messages.Count != 0)
                    {
                        DequeueMessages();
                    }
                    else
                    {
                        Info3 = "";
                    }
                }
            });
        }

        public void Close()
        {
            _closing = true;
            if (_ws.IsAlive)
            {
                _ws.Close();
            }

            if (_ipcServer.IsRunning)
            {
                _ipcServer.Stop();
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}
