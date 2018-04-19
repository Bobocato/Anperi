﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;
using WebSocketSharp;
using JJA.Anperi.Api;
using JJA.Anperi.Api.Shared;
using JJA.Anperi.DeviceApi;
using JJA.Anperi.Host.Utility;
using JJA.Anperi.HostApi;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json.Linq;

namespace JJA.Anperi.Host
{
    //TODO: rename GetPeripherals to Peripherals
    //TODO: change all OnPropertyChanged calls to OnPropertyChanged(nameof(Property)) eg: OnPropertyChanged(nameof(ButConnect)) makes it easier if you want to refactor stuff
    /*TODO: move all actions into actual actions (like we did in the bookmanager) and move all logic containing websocket calls to the model
      explanation: this is not a ViewModel ... this is effectively model and viewmodel in one class. the viewmodel should JUST CONVERT the model into displayable stuff and only contain
      most basic logic needed to map the window controls to the actual model. aka a button press will likely end up in a function call in the model (routed through the viewmodel) */
    //TODO: seperate DLL for the model
    //TODO: probably split model into multiple classes because this will get REALLY messy the moment IPC comes into play
    //TODO: speak to me about collections and bindings, this isn't gonna work like this (the GetPeripherals)
    class HostViewModel : INotifyPropertyChanged, INotifyCollectionChanged
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;
        private WebSocket _ws;
        private string _name = "";
        private string _filePath = "token.txt";
        private HostModel _model;

        public HostViewModel()
        {
            _model = new HostModel();
            if (File.Exists(_filePath))
            {
                _model.Token = File.ReadLines(_filePath).First();
            }
            InitializeWebSocket();
        }

        private void InitializeWebSocket()
        {
            Task.Run(() =>
            {
                using (_ws = new WebSocket(_model.WebSocketAddress))
                {
                    _ws.OnOpen += (sender, e) =>
                    {
                        if (_ws.IsAlive)
                        {
                            Info1 =
                                "Current WebSocket-Address is: " + _model.WebSocketAddress;
                        }
                        if (_model.Token.Equals(""))
                        {
                            var name = System.Environment.MachineName;
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
                                    _model.Token, SharedJsonDeviceType.host);
                            string msg = json.Serialize();
                            _ws.Send(msg); 
                        }
                    };

                    _ws.OnMessage += (sender, e) =>
                    {
                        Console.WriteLine(e.Data);
                        var json = JsonApiObject.Deserialize(e.Data);
                        var context = json.context;

                        if (context.Equals(JsonApiContextTypes.server))
                        {
                            if (Enum.IsDefined(typeof(SharedJsonRequestCode),
                                json.message_code))
                            {
                                Enum.TryParse<SharedJsonRequestCode>(
                                    json.message_code,
                                    out SharedJsonRequestCode type);
                                HandleSharedRequestCode(type, json);

                            }
                            else if (Enum.IsDefined(typeof(HostRequestCode),
                                json.message_code))
                            {
                                Enum.TryParse<HostRequestCode>(
                                    json.message_code,
                                    out HostRequestCode hostRequest);
                                HandleHostRequestCode(hostRequest, json);
                            }
                            else if (Enum.IsDefined(
                                typeof(SharedJsonMessageCode),
                                json.message_code))
                            {
                                Enum.TryParse<SharedJsonMessageCode>(
                                    json.message_code,
                                    out SharedJsonMessageCode sharedMessage);
                                HandleSharedMessageCode(sharedMessage, json);
                            }
                            else
                            {
                                ShowMessage("Unknown context!");
                            }
                        }
                    };

                    _ws.OnClose += (sender, e) =>
                    {                
                        Info1 = "No current WebSocket connection";
                        if (!_ws.IsAlive)
                        {
                            Thread.Sleep(2000);
                            _ws.Connect();
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

        private void HandleSharedMessageCode(SharedJsonMessageCode code, JsonApiObject json)
        {
            switch (code)
            {
                case SharedJsonMessageCode.error:
                    if (json.data.TryGetValue("msg", out dynamic error))
                    {
                        Console.WriteLine(error);
                    }
                    break;
                case SharedJsonMessageCode.partner_disconnected:
                    throw new NotImplementedException();
                    break;
            }
        }

        private void HandleSharedRequestCode(SharedJsonRequestCode code,
        JsonApiObject json)
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
                            Info3 = "Your name is: " + _name;
                        }
                        else
                        {
                            _model.Token = "";
                            ShowMessage("Login failed!");
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
                    _model.Token = token;
                    json.data.TryGetValue("name", out dynamic name);
                    _name = name;

                    var tokenStream = new StreamWriter(_filePath, false);
                    tokenStream.WriteLine(_model.Token);
                    tokenStream.Close();

                    SendPeripheralRequest();

                    Info2 = "Your name is: " + _name;
                    break;
                default:
                    throw new NotImplementedException(
                        $"Didnt implement: {code.ToString()}");
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
                            ShowMessage(
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
                    json.data.TryGetValue("devices",
                        out dynamic list);
                    if (list != null)
                    {
                        GetPeripherals.Clear();
                        foreach (var x in list)
                        {
                            JObject jo = x;
                            if (jo.TryGetCastValue("name", out string name) && jo.TryGetCastValue("id", out int id))
                            {
                                Peripherals.Add(new HostJsonApiObjectFactory.ApiPeripheral {id = id, name = name});
                                //GetPeripherals.Add(name, id);
                            }
                            else
                            {
                                Trace.TraceError($"couldn't parse get_available_peripherals answer: {list.ToString()}");
                            } 
                        }
                    }
                    else
                    {
                        Console.Write(
                            "Something is wrong with the send peripherals!");
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
                                ConnectedTo =
                                    "Connected to: " +
                                    json.data["id"];
                                //TODO: idea for connect/disconnect button
                                ButConnect = false;
                                ButDisconnect = true;
                            }
                            else
                            {
                                ConnectedTo =
                                    "Connected to:";
                                ButConnect = true;
                                ButDisconnect = false;
                            }
                        }
                        else
                        {
                            ShowMessage(
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

        private void SendPeripheralRequest()
        {
            var jsonPeri = HostJsonApiObjectFactory
                .CreateAvailablePeripheralRequest();
            SendToWebsocket(jsonPeri.Serialize());
        }

        public void Unpair(string name)
        {
            ShowMessage("Can't unpair, because no peripheral was selected!");
            var id = GetPeripherals[name];

            var json = HostJsonApiObjectFactory
                .CreateUnpairFromPeripheralRequest(id);
            SendToWebsocket(json.Serialize());
        }

        public void SendMessage(string message)
        {
            var json =
                DeviceJsonApiObjectFactory.CreateDebugRequest(message);
            SendToWebsocket(json.Serialize());
        }

        public void Connect(string name)
        {
            var id = GetPeripherals[name];
            var json = HostJsonApiObjectFactory
                .CreateConnectToPeripheralRequest(id);
            SendToWebsocket(json.Serialize());
        }

        public void Disconnect()
        {
            var json = HostJsonApiObjectFactory
                .CreateDisconnectFromPeripheralRequest();
            SendToWebsocket(json.Serialize());
        }

        public void Pair(string pairCode)
        {
            var json =
                HostJsonApiObjectFactory.CreatePairingRequest(pairCode);
            SendToWebsocket(json.Serialize());
        }

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
                    ShowMessage("Websocket is not alive!");
                }
            });
        }

        private void ShowMessage(string message)
        {
            Task.Run(() =>
            {
                Info3 = message;
                Thread.Sleep(3000);
                Info3 = "";
            });
        }
        
        public bool ButConnect
        {
            get { return _model.ButConnectVisible; }
            set
            {
                _model.ButConnectVisible = value;
                OnPropertyChanged("ButConnect");
            }
        }

        public bool ButDisconnect
        {
            get { return _model.ButDisconnectVisible; }
            set
            {
                _model.ButDisconnectVisible = value;
                OnPropertyChanged("ButDisconnect");
            }
        }

        public string Info1
        {
            get { return _model.Info1;}
            set
            {
                _model.Info1 = value;
                OnPropertyChanged("Info1");
            }
        }

        public string Info2
        {
            get { return _model.Info2; }
            set
            {
                _model.Info2 = value;
                OnPropertyChanged("Info2");
            }
        }

        public string Info3
        {
            get{ return _model.Info3; }
            set
            {
                _model.Info3 = value;
                OnPropertyChanged("Info3");
            }
        }

        public string ConnectedTo
        {
            get { return _model.ConnectedTo; }
            set
            {
                ConnectedTo = value;
                OnPropertyChanged("ConnectedTo");
            }
        }
        
        public ObservableCollection<HostJsonApiObjectFactory.ApiPeripheral> Peripherals => new ObservableCollection<HostJsonApiObjectFactory.ApiPeripheral>(new List<HostJsonApiObjectFactory.ApiPeripheral>());

        public Dictionary<string, int> GetPeripherals
        {
            get { return _model.Peripherals; }
            set
            {
                _model.Peripherals = value;
                //OnPropertyChanged("Peripherals");
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        public void Close()
        {
            if (_ws.IsAlive)
            {
                _ws.CloseAsync();
            }
        }

        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
        }


    }
}
