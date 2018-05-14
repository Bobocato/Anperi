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
using JJA.Anperi.Utility;
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
        private string _name = "";
        private bool _butDisconnectVisible = false;

        private bool _popupInput = false;
        private bool _popupOptions = false;

        private string _popupTitle = "";

        private string _wsAddress = "ws://localhost:5000/api/ws";
        //private string _wsAddress = "wss://anperi.jannes-peters.com/api/ws";
        private string _token = "";
        private int _favorite = -1;
        private WebSocket _ws;
        private List<HostJsonApiObjectFactory.ApiPeripheral> _periList;
        private readonly Queue<string> _messages;
        private bool _closing = false;
        private int _connectedPeripheral = -1;
        private NamedPipeIpcServer _ipcServer;
        private readonly List<IIpcClient> _ipcClients;
        private IIpcClient _curIpcClient;

        public HostModel(string token, int favorite)
        {
            Token = token;
            _favorite = favorite;
            _periList = new List<HostJsonApiObjectFactory.ApiPeripheral>();
            _ipcClients = new List<IIpcClient>();
            _messages = new Queue<string>();
            InitializeWebSocket();
            InitializeIpcServer();
        }

        #region Properties

        public string Info1
        {
            get => _info1;
            set
            {
                _info1 = value;
                OnPropertyChanged(nameof(Info1));
            }
        }
        public string Info2
        {
            get => _info2;
            set
            {
                _info2 = value;
                OnPropertyChanged(nameof(Info2));
            }
        }
        public string Info3
        {
            get => _info3;
            set
            {
                _info3 = value;
                OnPropertyChanged(nameof(Info3));
            }
        }
        public string ConnectedTo
        {
            get => _connectedTo;
            set
            {
                _connectedTo = value;
                ButDisconnect = value != "";
                if (_connectedTo.Equals(""))
                {
                    _connectedPeripheral = -1;
                }

                _ipcClients.AsParallel().ForAll(c => c.SendAsync(new IpcMessage(_connectedTo == "" ? IpcMessageCode.PeripheralDisconnected : IpcMessageCode.PeripheralConnected)));                
                OnPropertyChanged(nameof(ConnectedTo));
            }
        }

        public int Favorite
        {
            get => _favorite;
            set
            {
                _favorite = value;
                OnPropertyChanged();
            }
        }

        public List<HostJsonApiObjectFactory.ApiPeripheral> Peripherals
        {
            get => _periList;
            set
            {
                var tmp = from x in value orderby x.online descending select x;
                _periList = tmp.ToList();
                OnPropertyChanged(nameof(Peripherals));
            }
        }

        public string Token
        {
            get => _token;
            set
            {
                _token = value;
                OnPropertyChanged();
            }
        }

        public bool ButDisconnect
        {
            get => _butDisconnectVisible;
            set
            {
                _butDisconnectVisible = value;
                OnPropertyChanged(nameof(ButDisconnect));
            }
        }

        #region Popup

        public string PopupTitle
        {
            get => _popupTitle;
            set
            {
                _popupTitle = value;
                switch (value)
                {
                    case "rename":
                    case "message":
                    case "pair":
                        _popupOptions = false;
                        _popupInput = true;
                        break;
                    case "options":
                        _popupOptions = true;
                        _popupInput = false;
                        break;
                    default:
                        throw new NotImplementedException();
                }
                OnPropertyChanged();
            }
        }

        public bool PopupInput
        {
            get => _popupInput;
            set
            {
                _popupInput = value;
                OnPropertyChanged();
            }
        }

        public bool PopupOptions
        {
            get => _popupOptions;
            set
            {
                _popupOptions = value;
                OnPropertyChanged();
            }
        }

        #endregion

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
                    client.Message += (o, eventArgs) =>
                    {
                        IIpcClient senderClient = (IIpcClient) o;
                        var message = eventArgs.Message;
                        var messageType = message.MessageCode;

                        switch (messageType)
                        {
                            case IpcMessageCode.Debug:
                                if (message.Data.TryGetValue("msg", out string msg))
                                {
                                    QueueMessage(msg);
                                }
                                client.SendAsync(message);
                                break;
                            case IpcMessageCode.Unset:
                                client.SendAsync(message);
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
                            case IpcMessageCode.ClaimControl:
                                if (_curIpcClient != null)
                                {
                                    _curIpcClient?.SendAsync(new IpcMessage(IpcMessageCode.ControlLost));
                                    _curIpcClient = null;
                                }
                                _curIpcClient = (IIpcClient) o;
                                break;
                            case IpcMessageCode.FreeControl:
                                senderClient.SendAsync(new IpcMessage(IpcMessageCode.ControlLost));
                                _curIpcClient = null;
                                _ipcClients.AsParallel().ForAll(c => c.SendAsync(new IpcMessage(IpcMessageCode.NotClaimed)));
                                break;
                            case IpcMessageCode.PeripheralEventFired:
                            case IpcMessageCode.PeripheralDisconnected:
                            case IpcMessageCode.PeripheralConnected:
                            case IpcMessageCode.ControlLost:
                            case IpcMessageCode.NotClaimed:
                                //ignored
                                break;
                            case IpcMessageCode.Error:
                            default:
                                throw new NotImplementedException($"Didnt implement: {messageType}");
                        }
                    };
                    client.StartReceive();

                    if (_curIpcClient == null)
                    {
                        client.SendAsync(
                            new IpcMessage(IpcMessageCode.NotClaimed));
                    }

                    if (_connectedPeripheral != -1)
                    {
                        client.SendAsync(new IpcMessage(IpcMessageCode.PeripheralConnected));
                    }
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
                        if (Token.Equals(""))
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
                                    Token, SharedJsonDeviceType.host);
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
                                }else if (Enum.TryParse(json.message_code, out HostMessage hostMessage))
                                {
                                    HandleHostMessageCode(hostMessage, json);
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
                            default:
                                throw new NotImplementedException();
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
                        Console.WriteLine("Error in WebSocket connection: " + e.Exception);
                    };

                    _ws.Connect();
                }
            });
        }

        #region response handling

        private void HandleHostMessageCode(HostMessage code, JsonApiObject json)
        {
            switch (code)
            {
                case HostMessage.paired_peripheral_logged_on:
                case HostMessage.paired_peripheral_logged_off:
                    if (json.data.TryGetValue("id", out int id))
                    {
                        foreach (var x in Peripherals)
                        {
                            if (x.id != id) continue;
                            x.online = code == HostMessage.paired_peripheral_logged_on;
                            if (x.id == _connectedPeripheral)
                            {
                                ConnectedTo = "";
                            }
                            OnPropertyChanged(nameof(Peripherals));
                        }
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

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

                    if (_favorite != -1)
                    {
                        foreach (var x in Peripherals)
                        {
                            if (x.id == _favorite)
                            {
                                if (x.online)
                                {
                                    Connect(x);
                                }
                            }
                        }
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
                            }
                            else
                            {
                                ConnectedTo = "";
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
                case HostRequestCode.change_peripheral_name:
                    if (json.data.TryGetValue("success",
                        out dynamic renameSuccess))
                    {
                        if (renameSuccess)
                        {
                            if (json.data.TryGetValue("id", out int id))
                            {
                                foreach (var x in Peripherals)
                                {
                                    if (x.id != id) continue;
                                    if (json.data.TryGetValue("name", out string name))
                                    {
                                        x.name = name;
                                        OnPropertyChanged(nameof(Peripherals));
                                    }
                                    else
                                    {
                                        Trace.TraceWarning(
                                            "name missed in rename response");
                                    }
                                }
                            }
                            else
                            {
                                Trace.TraceWarning(
                                    "id missed in rename response");
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
                            "success missed in rename response");
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(code), code, null);
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
                            Token = "";
                            QueueMessage("Login failed!");
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
                    Token = token;
                    json.data.TryGetValue("name", out dynamic name);
                    _name = name;

                    SendPeripheralRequest();

                    Info2 = "Your name is: " + _name;
                    break;
                case SharedJsonRequestCode.set_own_name:
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
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(code), code, null);
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
                    var getInfo = new IpcMessage(IpcMessageCode.GetPeripheralInfo)
                    {
                        Data = json.data                       
                    };
                    _curIpcClient?.SendAsync(getInfo);
                    break;
                case DeviceRequestCode.event_fired:
                    var eventFired = new IpcMessage(IpcMessageCode.PeripheralEventFired)
                    {
                        Data = json.data
                    };
                    _curIpcClient?.SendAsync(eventFired);
                    break;
                case DeviceRequestCode.error:
                    var errorMessage = new IpcMessage(IpcMessageCode.Error)
                    {
                        Data = json.data
                    };
                    _curIpcClient?.SendAsync(errorMessage);
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
            if (id == _connectedPeripheral)
            {
                Disconnect();
            }
            var json = HostJsonApiObjectFactory.CreateUnpairFromPeripheralRequest(id);
            SendToWebsocket(json.Serialize());
        }

        public void Rename(int id, string name)
        {
            if (id == -1)
            {
                Console.WriteLine("Can't send to id -1!");
                return;
            }
            var json =
                HostJsonApiObjectFactory.CreateChangeNameRequest(id,
                    name);
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
