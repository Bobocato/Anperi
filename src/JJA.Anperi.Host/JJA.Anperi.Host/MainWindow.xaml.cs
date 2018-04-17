using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Threading;
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
        //TODO: write address in config
        //private string _wsAddress = "ws://localhost:5000/api/ws";
        private string _wsAddress = "wss://anperi.jannes-peters.com/api/ws";
        private WebSocket _ws;
        private string _token = "";
        private string _name = "";

        public MainWindow()
        {
            InitializeComponent();
            //TODO: read token from file
            InitializeWebSocket();
        }

        private void InitializeWebSocket()
        {
            //InfoBlock.Text = "Trying to connect to: " + _wsAddress;
            Task.Run(() =>
            {
                using (_ws = new WebSocket(_wsAddress))
                {
                    _ws.OnOpen += (sender, e) =>
                    {
                        this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                        {
                            if (_ws.IsAlive)
                            {
                                InfoBlock.Text = "Current WebSocket-Address is: " + _wsAddress;
                            }                            
                        }));                      
                        if (_token.Equals(""))
                        {
                            var name = System.Environment.MachineName;
                            var json =
                                SharedJsonApiObjectFactory
                                    .CreateRegisterRequest(
                                        SharedJsonDeviceType.host, name);
                            _ws.Send(json.Serialize());
                        }else
                        {
                            var json =
                                SharedJsonApiObjectFactory.CreateLoginRequest(
                                    _token, SharedJsonDeviceType.host);
                            _ws.Send(json.Serialize());
                        }
                    };
                    
                    _ws.OnMessage += (sender, e) =>
                    {    
                        Console.Write(e.Data);
                        var json = JsonApiObject.Deserialize(e.Data);
                        var context = json.context;

                        if (context.Equals(JsonApiContextTypes.server))
                        {
                            switch (json.message_code)
                            {
                                case "pair":
                                case "unpair":
                                    if (json.data["success"])
                                    {
                                        SendPeripheralRequest();
                                    }
                                    else
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
                                            this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                                            {
                                                BlockConnected.Text = "Connected to: " + json.data["id"];
                                                ButConnect.Visibility =
                                                    Visibility.Hidden;
                                                ButDisconnect.Visibility =
                                                    Visibility.Visible;
                                            }));
                                        }else
                                        {
                                            this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                                            {
                                                BlockConnected.Text = "Connected to:";
                                                ButConnect.Visibility =
                                                    Visibility.Visible;
                                                ButDisconnect.Visibility =
                                                    Visibility.Hidden;
                                            }));
                                        }
                                    }else
                                    {
                                        ShowMessage("Something went wrong in operation " + json.message_code + " !");
                                    }
                                    break;
                                case "partner_disconnected":
                                    this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                                    {                                     
                                        BlockConnected.Text = "Connected to:";
                                        ShowMessage("Device disconnected!");
                                        ButConnect.Visibility = Visibility.Visible;
                                        ButDisconnect.Visibility =
                                            Visibility.Hidden;
                                    }));
                                    break;
                                case "error":
                                    var msg = json.data["msg"];
                                    ShowMessage("WebSocket send error: " + msg);
                                    break;
                                case "register":
                                    _token = json.data["token"];
                                    //TODO: write token to file
                                    _name = json.data["name"];
                                    this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                                    {
                                        InfoBlock2.Text = "Your name is: " + _name;
                                    }));
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
                                    //TODO: finish this
                                    var dictionary = json.data["devices"];
                                    foreach (var x in dictionary)
                                    {
                                        ListBoxItem item = new ListBoxItem();
                                        //add items like "1234567,Test"
                                    }
                                    break;
                            }
                        }else if (context.Equals(JsonApiContextTypes.device))
                        {
                            //TODO: write cases for device
                        }else
                        {
                            ShowMessage("Unknown context!");
                        }
                    };

                    _ws.OnClose += (sender, e) =>
                    {
                        this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                        {
                            InfoBlock.Text = "No current WebSocket connection";
                        }));

                        if (!e.WasClean)
                        {
                            if (!_ws.IsAlive)
                            {
                                _token = "";
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
                this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        InfoBlock.Text = message;
                    }));
                Thread.Sleep(1500);
                this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                {
                    InfoBlock.Text = tmp;
                }));
            });            
        }

        private void SendToWebsocket(string message)
        {
            Task.Run(() =>
            {
                if (_ws.IsAlive)
                {
                    _ws.Send(message);
                }else
                {
                    ShowMessage("Websocket is not alive!");
                }
            });
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
            if (PeriBox.Items.Count == 0)
            {
                ShowMessage("Can't connect, because the list is empty!");
            }

            if (PeriBox.SelectedIndex == -1)
            {
                ShowMessage("Can't connect, because no peripheral was selected!");
            }else
            {
                var curItem = PeriBox.SelectedItem.ToString();
                var substrings = curItem.Split(',');
                var id = Int32.Parse(substrings[0]);
                var json = HostJsonApiObjectFactory.CreateConnectToPeripheralRequest(id);
                SendToWebsocket(json.Serialize());
            }
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
            if (PeriBox.Items.Count == 0)
            {
                ShowMessage("Can't unpair, because the list is empty!");
            }

            if (PeriBox.SelectedIndex == -1)
            {
                ShowMessage("Can't unpair, because no peripheral was selected!");
            }else
            {
                var curItem = PeriBox.SelectedItem.ToString();
                var substrings = curItem.Split(',');
                var id = Int32.Parse(substrings[0]);
                var json = HostJsonApiObjectFactory.CreateUnpairFromPeripheralRequest(id);
                SendToWebsocket(json.Serialize());
            }
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (_ws.IsAlive)
            {
                _ws.CloseAsync();
            }
        }
    }
}
