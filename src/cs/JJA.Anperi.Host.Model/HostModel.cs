using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using JJA.Anperi.Host.Model.Annotations;
using JJA.Anperi.Host.Model.Utility;
using WebSocketSharp;
using JJA.Anperi.Internal.Api;
using JJA.Anperi.Internal.Api.Device;
using JJA.Anperi.Internal.Api.Host;
using JJA.Anperi.Internal.Api.Shared;
using JJA.Anperi.Ipc.Dto;
using JJA.Anperi.Ipc.Server;
using JJA.Anperi.Ipc.Server.NamedPipe;
using JJA.Anperi.Utility;
using Newtonsoft.Json.Linq;


namespace JJA.Anperi.Host.Model
{
    public class HostModel : INotifyPropertyChanged, IDisposable
    {
        public static HostModel Instance => _instance.Value;
        private static Lazy<HostModel> _instance = new Lazy<HostModel>(() => new HostModel());

        public event PropertyChangedEventHandler PropertyChanged;
        
        private WebSocket _ws;
        private List<Peripheral> _periList;
        private readonly BlockingCollection<string> _messages;
        private bool _closing = false;
        private Peripheral _connectedPeripheral = null;
        private int _connectToPeripheral = -1;
        private NamedPipeIpcServer _ipcServer;
        private readonly List<IIpcClient> _ipcClients;
        private IIpcClient _curIpcClient;
        private string _ownName;
        private string _serverAddress;
        private bool _isPeripheralListLoaded = false;
        private string _message;
        private readonly CancellationTokenSource _closeTokenSource;

        public HostModel()
        {
            _periList = new List<Peripheral>();
            _ipcClients = new List<IIpcClient>();
            _messages = new BlockingCollection<string>(new ConcurrentQueue<string>());
            _closeTokenSource = new CancellationTokenSource();
            InitializeWebSocket();
            InitializeIpcServer();
            DequeueMessages();
        }

        #region Properties

        public static string DefaultServer => "wss://anperi.jannes-peters.com/api/ws";

        public string Message
        {
            get => _message;
            private set
            {
                if (_message != value)
                {
                    _message = value;
                    OnPropertyChanged();
                }
            }
        }

        public string OwnName
        {
            get => _ownName;
            private set
            {
                if (_ownName != value)
                {
                    _ownName = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ServerAddress
        {
            get { return _serverAddress; }
            private set
            {
                if (_serverAddress != value)
                {
                    _serverAddress = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsPeripheralListLoaded
        {
            get => _isPeripheralListLoaded;
            private set
            {
                if (_isPeripheralListLoaded != value)
                {
                    _isPeripheralListLoaded = value;
                    OnPropertyChanged();
                }
            }
        }

        public Peripheral ConnectedPeripheral
        {
            get => _connectedPeripheral;
            private set
            {
                if (!Equals(value, _connectedPeripheral))
                {
                    if (_connectedPeripheral != null) _connectedPeripheral.IsConnected = false;
                    _connectedPeripheral = value;
                    IpcMessageCode ipcMsg = IpcMessageCode.PeripheralDisconnected;
                    if (_connectedPeripheral != null)
                    {
                        _connectedPeripheral.IsConnected = true;
                        ipcMsg = IpcMessageCode.PeripheralConnected;
                    }
                    OnPropertyChanged();
                    _ipcClients.AsParallel().ForAll(c => c.SendAsync(new IpcMessage(ipcMsg)));
                }
            }
        }

        public int Favorite
        {
            get => ConfigHandler.Load().Favorite;
            set
            {
                if (ConfigHandler.Load().Favorite != value)
                {
                    ConfigHandler.Load().Favorite = value;
                    Peripherals.ForEach(p => p.IsFavorite = false);
                    Peripheral peri = Peripherals.SingleOrDefault(p => p.Id == ConfigHandler.Load().Favorite);
                    if (peri != null) peri.IsFavorite = true;
                    OnPropertyChanged();
                }
            }
        }

        public List<Peripheral> Peripherals
        {
            get => _periList;
            private set
            {
                var tmp = from x in value orderby x.Online descending select x;
                _periList = tmp.ToList();
                OnPropertyChanged(nameof(Peripherals));
            }
        }

        public bool IsConnected => _ws.IsAlive;

        #endregion

        private void InitializeIpcServer()
        {
            _ipcServer?.Dispose();
            _ipcServer = new NamedPipeIpcServer();               
            _ipcServer.ClientConnected += (sender, args) =>
            {
                var client = args.Client;
                _ipcClients.Add(client);
                client.Message += (o, eventArgs) =>
                {
                    IIpcClient senderClient = (IIpcClient) o;
                    Trace.TraceInformation("Received on IPC client {0} - {1}: {2}", senderClient.Id, eventArgs.Message.MessageCode.ToString(), eventArgs.Message.Data?.ToDataString());
                    var message = eventArgs.Message;
                    var messageType = message.MessageCode;

                    switch (messageType)
                    {
                        case IpcMessageCode.Debug:
                            if (message.Data.TryGetValue("msg", out string msg))
                            {
#if DEBUG
                                QueueMessage(msg);
#endif
                            }
                            client.SendAsync(message);
                            break;
                        case IpcMessageCode.Unset:
                            client.SendAsync(message);
                            break;
                        case IpcMessageCode.GetPeripheralInfo:
                            SendToWebsocket(DeviceJsonApiObjectFactory.CreateGetInfo().Serialize());
                            break;
                        case IpcMessageCode.SetPeripheralElementParam:
                            if (message.Data == null)
                            {
                                Trace.TraceWarning("Got empty element param data from client.");
                                return;
                            }
                            SendToWebsocket(DeviceJsonApiObjectFactory.CreateSetElementParam(message.Data).Serialize());
                            break;
                        case IpcMessageCode.SetPeripheralLayout:
                            if (message.Data == null)
                            {
                                Trace.TraceWarning("Got empty set_layout data from client.");
                                return;
                            }
                            SendToWebsocket(DeviceJsonApiObjectFactory.CreateSetLayout(message.Data).Serialize());
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
                            SendToWebsocket(DeviceJsonApiObjectFactory.CreateDeviceWentAway().Serialize());
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
                            Trace.TraceError("Error from IpcMessage: {0}", message.Data.ToDataString());
                            break;
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

                if (ConnectedPeripheral != null)
                {
                    client.SendAsync(new IpcMessage(IpcMessageCode.PeripheralConnected));
                }
            };

            _ipcServer.ClientDisconnected += (sender, args) =>
            {
                var client = args.Client;
                _ipcClients.Remove(client);
                if (client.Equals(_curIpcClient))
                {
                    _curIpcClient = null;
                    SendToWebsocket(DeviceJsonApiObjectFactory.CreateDeviceWentAway().Serialize());
                }
            };

            _ipcServer.Closed += async (sender, args) =>
            {
                _ipcClients.ForEach(x => x.Close());
                _ipcClients.Clear();
                if (!_closing)
                {
                    Trace.TraceError("Ipc Server is restarting in 2 seconds!");
                    await Task.Delay(2000);
                    _ipcServer.Start();
                }
            };

            _ipcServer.Error += (sender, args) =>
            {
                Trace.TraceError("IPC-Server error: " + args.ToString());
            };

            _ipcServer.Start();
            Trace.TraceInformation("Ipc-Server is running!");
        }
        
        private void InitializeWebSocket()
        {
            Task.Run(() =>
            {
                (_ws as IDisposable)?.Dispose();
                ServerAddress = ConfigHandler.Load().ServerAddress;
                _ws = new WebSocket(ServerAddress);
                _ws.OnOpen += (sender, e) =>
                {
                    if (string.IsNullOrEmpty(ConfigHandler.Load().Token))
                    {
                        var name = Environment.MachineName;
                        var json =
                            SharedJsonApiObjectFactory
                                .CreateRegisterRequest(
                                    SharedJsonDeviceType.host, name);
                        SendToWebsocket(json.Serialize());
                    }
                    else
                    {
                        var json =
                            SharedJsonApiObjectFactory.CreateLoginRequest(
                                ConfigHandler.Load().Token, SharedJsonDeviceType.host);
                        string msg = json.Serialize();
                        Trace.TraceInformation("Sending login request to server.");
                        _ws.Send(msg);
                    }
                    OnPropertyChanged(nameof(IsConnected));
                };

                _ws.OnMessage += (sender, e) =>
                {
                    Trace.TraceInformation(e.Data);
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
                            else if (Enum.TryParse(json.message_code, out HostMessage hostMessage))
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
#if DEBUG
                            throw new NotImplementedException();
#else
                            break;
#endif
                    }
                };

                _ws.OnClose += (sender, e) =>
                {
                    if (!_closing)
                    {
                        ResetUi();
                        if (!_ws.IsAlive)
                        {
                            Thread.Sleep(2000);
                            _ws.Connect();
                        }
                        OnPropertyChanged(nameof(IsConnected));
                    }
                };

                _ws.OnError += (sender, e) =>
                {
                    Util.TraceException("Error in WebSocket connection", e.Exception);
                    ResetUi();
                };

                _ws.Connect();
            });
        }

        #region response handling

        private void ResetUi()
        {
            //the ui should reset here to handle a disconnect
            IsPeripheralListLoaded = false;
            ConnectedPeripheral = null;
            Peripherals.Clear();

            //temporarily fix, the Clear-method does not invoke the setter
            OnPropertyChanged(nameof(Peripherals));
            OnPropertyChanged(nameof(IsConnected));
        }

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
                            if (x.Id != id) continue;
                            x.Online = code == HostMessage.paired_peripheral_logged_on;
                            if (x.Id == ConnectedPeripheral?.Id)
                            {
                                ConnectedPeripheral = null;
                            }
                            OnPropertyChanged(nameof(Peripherals));
                        }

                        if (code == HostMessage.paired_peripheral_logged_on && ConnectedPeripheral == null && id == Favorite)
                        {
                            var connectRequest = HostJsonApiObjectFactory
                                .CreateConnectToPeripheralRequest(id);
                            SendToWebsocket(connectRequest.Serialize());
                        }
                    }
                    break;
                default:
#if DEBUG
                            throw new NotImplementedException();
#else
                    break;
#endif
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
                            if (code == HostRequestCode.pair && json.data.TryGetValue("id", out int id) && ConnectedPeripheral == null)
                            {
                                _connectToPeripheral = id;
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
                                Peripheral p = new Peripheral(id, name, online);
                                if (p.Id == Favorite) p.IsFavorite = true;
                                Peripherals.Add(p);
                            }
                            else
                            {
                                Trace.TraceError($"couldn't parse get_available_peripherals answer: {list.ToString()}");
                            }
                        }
                        OnPropertyChanged(nameof(Peripherals));
                        IsPeripheralListLoaded = true;
                        if (_connectToPeripheral != -1)
                        {
                            foreach (var x in Peripherals)
                            {
                                if (x.Id != _connectToPeripheral || !x.Online)
                                    continue;
                                var connectRequest = HostJsonApiObjectFactory
                                    .CreateConnectToPeripheralRequest(x.Id);
                                SendToWebsocket(connectRequest.Serialize());
                            }
                            _connectToPeripheral = -1;
                        }else
                        {
                            Peripheral peri = null;
                            foreach (var x in Peripherals)
                            {
                                if (!x.Online) continue;
                                if (peri == null)
                                {
                                    peri = x;
                                }else
                                {
                                    peri = null;
                                    break;
                                }
                            }
                            if (peri != null)
                            {
                                var connectRequest = HostJsonApiObjectFactory
                                    .CreateConnectToPeripheralRequest(peri.Id);
                                SendToWebsocket(connectRequest.Serialize());
                            }
                        }
                    }
                    else
                    {
                        Trace.TraceWarning("Something is wrong with the send peripherals!");
                    }

                    if (Favorite != -1 && ConnectedPeripheral == null)
                    {
                        foreach (var x in Peripherals)
                        {
                            if (x.Id == Favorite)
                            {
                                if (x.Online)
                                {
                                    Connect(x);
                                }
                            }
                        }
                    }

                    break;
                case HostRequestCode.connect_to_peripheral:
                    if (json.data.TryGetValue("success", out bool connectSuccess))
                    {
                        if (connectSuccess && json.data.TryGetValue("id", out int connectId))
                        {
                            ConnectedPeripheral = Peripherals.SingleOrDefault(p => p.Id == connectId);
                            if (ConnectedPeripheral == null)
                            {
                                QueueMessage("Couldn't find the connected device in peripheral list!");
                            }
                        }
                    }
                    else
                    {
                        Trace.TraceWarning("Connect response missed data: {0}", json.data.ToDataString());
                    }
                    break;
                case HostRequestCode.disconnect_from_peripheral:
                    if (json.data.TryGetValue("success", out bool disconnectSuccess))
                    {
                        if (disconnectSuccess) ConnectedPeripheral = null;
                    }
                    else
                    {
                        Trace.TraceWarning("Disconnect response missed data: {0}", json.data.ToDataString());
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
                                    if (x.Id != id) continue;
                                    if (json.data.TryGetValue("name", out string name))
                                    {
                                        x.Name = name;
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
                            OwnName = loginName;
                            SendPeripheralRequest();
                        }
                        else
                        {
                            ConfigHandler.Load().Token = "";
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
                    ConfigHandler.Load().Token = token;
                    json.data.TryGetValue("name", out dynamic name);
                    OwnName = name;

                    SendPeripheralRequest();
                    break;
                case SharedJsonRequestCode.set_own_name:
                    json.data.TryGetValue("name", out string newName);
                    OwnName = newName;
                    break;
                default:
#if DEBUG
                    throw new NotImplementedException($"Didnt implement: {code.ToString()}");
#else
                    break;
#endif
            }
        }

        private void HandleSharedMessageCode(SharedJsonMessageCode code, JsonApiObject json)
        {
            switch (code)
            {
                case SharedJsonMessageCode.error:
                    if (json.data.TryGetValue("message", out string error))
                    {
                        Trace.TraceWarning(error);
                        QueueMessage(error);
                    }
                    break;
                case SharedJsonMessageCode.partner_disconnected:
                    ConnectedPeripheral = null;
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
            if (!(item is Peripheral peripheral)) throw new ArgumentException("The given item wasn't the required type Peripheral", nameof(item));
            if (peripheral.Id == ConnectedPeripheral?.Id)
            {
                Disconnect();
            }
            var json = HostJsonApiObjectFactory.CreateUnpairFromPeripheralRequest(peripheral.Id);
            SendToWebsocket(json.Serialize());
        }

        public void Rename(int id, string name)
        {
            if (id == -1)
            {
                Trace.TraceInformation("Can't send to id -1!");
                return;
            }
            var json =
                HostJsonApiObjectFactory.CreateChangeNameRequest(id,
                    name);
            SendToWebsocket(json.Serialize());
        }

        public void RenameSelf(string name)
        {
            var json = SharedJsonApiObjectFactory.CreateChangeOwnNameRequest(name);
            SendToWebsocket(json.Serialize());
        }

        public void SendMessage(string message)
        {
            var json =
                DeviceJsonApiObjectFactory.CreateDebug(message);
            SendToWebsocket(json.Serialize());
        }

        public void Connect(Peripheral peripheral)
        {
            if (peripheral == null) throw new ArgumentNullException(nameof(peripheral));
            var json = HostJsonApiObjectFactory.CreateConnectToPeripheralRequest(peripheral.Id);
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
                    Trace.TraceInformation("Sending To Websocket: {0}", message);
                    _ws.Send(message);
                }
                else
                {
                    Trace.TraceWarning("Tried to send \n{0}\n\tto websocket, but it was closed.", message);
                    QueueMessage("Websocket is not alive!");
                }
            });
        }

        private void QueueMessage(string message)
        {
            Trace.TraceInformation("QueueMessage got: {0}", message);
            _messages.Add(message);
        }

        private async void DequeueMessages()
        {
            while (!_closeTokenSource.IsCancellationRequested)
            {
                try
                {
                    string msg = await Task.Run(() => _messages.Take(_closeTokenSource.Token)).ConfigureAwait(false);
                    Message = msg;
                    await Task.Delay(3000, _closeTokenSource.Token).ConfigureAwait(false);
                } catch(OperationCanceledException) { }
                if (_messages.Count == 0)
                {
                    Message = "";
                }
            }
        }

        public void Close()
        {
            _closing = true;
            _closeTokenSource.Cancel();
            if (_ws.IsAlive)
            {
                _ws.Close();
            }

            if (_ipcServer.IsRunning)
            {
                _ipcServer.Stop();
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this,
                new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            ((IDisposable) _ws)?.Dispose();
            _ipcServer?.Dispose();
        }
    }
}
