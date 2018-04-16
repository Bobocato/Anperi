using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json;
using WebSocketSharp;
using JJA.Anperi.Api;
using JJA.Anperi.Api.Shared;
using JJA.Anperi.DeviceApi;
using JJA.Anperi.HostApi;

namespace JJA.Anperi.Host
{
    public partial class MainWindow : Window
    {
        private string _wsAddress = "ws://localhost:5000/api/ws";
        private WebSocket _ws;
        private string token = "";

        public MainWindow()
        {
            InitializeComponent();
            InitializeWebSocket();
        }

        private void InitializeWebSocket()
        {
            InfoBlock.Text = "Trying to connect to: " + _wsAddress;
            Task.Run(() =>
            {
                using (_ws = new WebSocket(_wsAddress))
                {
                    _ws.OnOpen += (sender, e) =>
                    {
                        InfoBlock.Text = "Current WebSocket-Address is: " + _wsAddress;

                        if (token.Equals(""))
                        {
                            var jsonReg =
                                SharedJsonApiObjectFactory
                                    .CreateRegisterRequest(SharedJsonDeviceType
                                        .host);
                            SendToWebsocket(jsonReg.Serialize());
                        }
                    };
                    
                    _ws.OnMessage += (sender, e) =>
                    {    
                        Console.Write(e.Data);
                        var json = JsonApiObject.Deserialize(e.Data);
                        var context = json.context.ToString();

                        if (context.Equals("server"))
                        {
                            switch (json.message_code)
                            {
                                case "pair":
                                case "unpair":
                                    if (json.data["success"])
                                    {
                                        SendPeripheralRequest();
                                    }else
                                    {
                                        ShowMessage("Something went wrong in operation " + json.message_code + " !");
                                    }
                                    break;
                                case "connect_to_peripheral":
                                case "disconnect_ from _peripheral":
                                    if (json.data["success"])
                                    {
                                        if (json.message_code.Equals("connect_to_peripheral"))
                                        {
                                            BlockConnected.Text = "Connected to: " + json.data["id"];
                                        }else
                                        {
                                            BlockConnected.Text = "Connected to:";
                                        }
                                    }else
                                    {
                                        ShowMessage("Something went wrong in operation " + json.message_code + " !");
                                    }
                                    break;
                                case "partner_disconnected":
                                    BlockConnected.Text = "Connected to:";
                                    ShowMessage("Device disconnected!");
                                    break;
                                case "error":
                                    var msg = json.data["msg"];
                                    ShowMessage("WebSocket send error: " + msg);
                                    break;
                                case "register":
                                    token = json.data["token"];
                                    SendPeripheralRequest();
                                    break;
                                case "login":
                                    if (json.data["success"])
                                    {
                                        SendPeripheralRequest();
                                    }else
                                    {
                                        ShowMessage("Login failed!");
                                    }
                                    break;
                                case "get_available_peripherals":
                                    //TODO: fix this
                                    var dictionary = json.data["devices"];
                                    foreach (var x in dictionary)
                                    {
                                        ListBoxItem item = new ListBoxItem();
                                        //add items like "id:1234567,name:Test"
                                    }
                                    break;
                            }
                        }else if (context.Equals("device"))
                        {
                            //TODO: write cases for device
                        }else
                        {
                            ShowMessage("Unknown context!");
                        }
                    };

                    _ws.OnClose += (sender, e) =>
                    {
                        InfoBlock.Text = "No current WebSocket connection";
                        if (!e.WasClean)
                        {
                            if (!_ws.IsAlive)
                            {
                                token = "";
                                Thread.Sleep(5000);
                                _ws.Connect();
                            }
                        }
                    };
                }
                _ws.Connect();
            });
        }

        private void ShowMessage(string message)
        {
            Task.Run(() =>
            {
                var tmp = InfoBlock.Text;
                InfoBlock.Text = message;
                Thread.Sleep(3000);
                InfoBlock.Text = tmp;
            });            
        }

        private void SendToWebsocket(string message)
        {
            if (_ws.IsAlive)
            {
                _ws.Send(message);
            }else
            {
                ShowMessage("Websocket is not alive!");
            }
        }

        private void SendPeripheralRequest()
        {
            var jsonPeri = HostJsonApiObjectFactory
                .CreateAvailablePeripheralRequest();
            SendToWebsocket(jsonPeri.Serialize());
        }

        private void ButPair_Click(object sender, RoutedEventArgs e)
        {
            string pairCode = CodeBox.Text;
            var json =
                HostJsonApiObjectFactory.CreatePairingRequest(pairCode);
            SendToWebsocket(json.Serialize());
        }

        private void ButConnect_Click(object sender, RoutedEventArgs e)
        {
            var curItem = PeriBox.SelectedItem.ToString();
            var substrings = curItem.Split(',');
            var id = Int32.Parse(substrings[0].Substring(3));
            var json =
                HostJsonApiObjectFactory.CreateConnectToPeripheralRequest(id);
            SendToWebsocket(json.Serialize());
        }

        private void ButSendMessage_Click(object sender, RoutedEventArgs e)
        {
            string message = MessageBox.Text;
            var json = DeviceJsonApiObjectFactory.CreateDebugRequest(message);
            SendToWebsocket(json.Serialize());
        }

        private void ButDisconnect_Click(object sender, RoutedEventArgs e)
        {
            var json = HostJsonApiObjectFactory
                .CreateDisconnectFromPeripheralRequest();
            SendToWebsocket(json.Serialize());
        }

        private void ButUnpair_Click(object sender, RoutedEventArgs e)
        {
            var curItem = PeriBox.SelectedItem.ToString();
            var substrings = curItem.Split(',');
            var id = Int32.Parse(substrings[0].Substring(3));
            //TODO: send a JSON object for unpairing with choosen device, when implemented
        }
    }
}
