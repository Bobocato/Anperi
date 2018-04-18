using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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
        private string _wsAddress = "ws://localhost:5000/api/ws";
        //private string _wsAddress = "wss://anperi.jannes-peters.com/api/ws";
        private WebSocket _ws;
        private string _token = "";
        private string _name = "";
        private string _filePath = "token.txt";

        public MainWindow()
        {
            InitializeComponent();
            if (File.Exists(_filePath))
            {
                _token = File.ReadLines(_filePath).First();
            }
            InitializeWebSocket();
        }

        private void InitializeWebSocket()
        {
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
                        }
                        else
                        {
                            var json =
                                SharedJsonApiObjectFactory.CreateLoginRequest(
                                    _token, SharedJsonDeviceType.host);
                            string msg = json.Serialize();
                            _ws.Send(msg); //TODO: here getting DC, why?
                        }
                    };

                    _ws.OnMessage += (sender, e) =>
                    {
                        Console.Write(e.Data);
                        var json = JsonApiObject.Deserialize(e.Data);
                        var context = json.context;

                        if (context.Equals(JsonApiContextTypes.server))
                        {
                            if (Enum.IsDefined(typeof(SharedJsonRequestCode), json.message_code))
                            {
                                Enum.TryParse<SharedJsonRequestCode>(json.message_code, out SharedJsonRequestCode type);
                                HandleSharedRequestCode(type, json);
                                
                            }else if (Enum.IsDefined(typeof(HostRequestCode), json.message_code))
                            {
                                Enum.TryParse<HostRequestCode>(json.message_code,out HostRequestCode hostRequest);
                                HandleHostRequestCode(hostRequest, json);
                            }
                        }
                        else
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

                        if (!_ws.IsAlive)
                        {
                            Thread.Sleep(2000);
                            _ws.Connect();
                        }
                    };

                    _ws.OnError += (sender, e) =>
                    {
                        Console.WriteLine("test");
                    };

                    _ws.Connect();
                }
            });
        }

        private void HandleSharedRequestCode(SharedJsonRequestCode code, JsonApiObject json)
        {
            switch (code)
            {
                case SharedJsonRequestCode.login:
                    if (json.data.TryGetValue("success", out dynamic success))
                    {
                        if (success)
                        {
                            json.data.TryGetValue("name", out dynamic loginName);
                            _name = loginName;
                            this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                            {
                                InfoBlock2.Text = "Your name is: " + _name;
                            }));
                            SendPeripheralRequest();
                        }
                        else
                        {
                            _token = "";
                            ShowMessage("Login failed!");
                        }
                    }
                    else
                    {
                        Trace.TraceWarning("success missed in login response");
                    }
                    break;
                case SharedJsonRequestCode.register:
                    json.data.TryGetValue("token", out dynamic token);
                    _token = token;
                    json.data.TryGetValue("name", out dynamic name);
                    _name = name;
                    this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        InfoBlock2.Text = "Your name is: " + _name;
                    }));

                    var tokenStream = new StreamWriter(_filePath, false);
                    tokenStream.WriteLine(_token);
                    tokenStream.Close();

                    SendPeripheralRequest();
                    break;
                default:
                    throw new NotImplementedException($"Didnt implement: {code.ToString()}");
            }
        }

        private void HandleHostRequestCode(HostRequestCode code, JsonApiObject json)
        {
            switch (code)
            {
                case HostRequestCode.pair:
                case HostRequestCode.unpair:
                    if (json.data.TryGetValue("success", out dynamic pairSuccess))
                    {
                        if (pairSuccess)
                        {
                            SendPeripheralRequest();
                        }
                        else
                        {
                            ShowMessage("Something went wrong in operation " + json.message_code + " !");
                        }
                    }
                    else
                    {
                        Trace.TraceWarning("success missed in pair/unpair response");
                    }
                    break;
                case HostRequestCode.get_available_peripherals:
                    //TODO: finish this
                    json.data.TryGetValue("devices", out dynamic dictionary);
                    if (dictionary != null)
                    {
                        this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                        {
                            PeriBox.Items.Clear();
                            foreach (var x in dictionary)
                            {
                                var item = new ListBoxItem
                                {
                                    Content = x.Key + "," + x.Value
                                };
                                PeriBox.Items.Add(item);
                                //add items like "1234567,Test"
                            }
                        }));

                    }
                    else
                    {
                        Console.Write("Something is wrong with the send peripherals!");
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
                                this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                                {
                                    BlockConnected.Text = "Connected to: " + json.data["id"];
                                    ButConnect.Visibility =
                                        Visibility.Hidden;
                                    ButDisconnect.Visibility
                                        =
                                        Visibility.Visible;
                                }));
                            }
                            else
                            {
                                this.Dispatcher.Invoke(
                                    DispatcherPriority.Normal,
                                    new Action(() =>
                                    {
                                        BlockConnected.Text =
                                            "Connected to:";
                                        ButConnect.Visibility =
                                            Visibility.Visible;
                                        ButDisconnect.Visibility
                                            =
                                            Visibility.Hidden;
                                    }));
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
                        Trace.TraceWarning("success missed in connect/disconnect response");
                    }
                    break;
            }
        }

        private void ShowMessage(string message)
        {
            Task.Run(() =>
            {
                this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        InfoBlock3.Text = message;                      
                    }));
                Thread.Sleep(3000);
                this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                {
                    InfoBlock3.Text = "";
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
            else
            {
                if (PeriBox.SelectedIndex == -1)
                {
                    ShowMessage("Can't connect, because no peripheral was selected!");
                }
                else
                {
                    var curItem = PeriBox.SelectedItem.ToString();
                    var substrings = curItem.Split(',');
                    var id = Int32.Parse(substrings[0]);
                    var json = HostJsonApiObjectFactory.CreateConnectToPeripheralRequest(id);
                    SendToWebsocket(json.Serialize());
                }
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
            else
            {
                if (PeriBox.SelectedIndex == -1)
                {
                    ShowMessage("Can't unpair, because no peripheral was selected!");
                }
                else
                {
                    var curItem = PeriBox.SelectedItem.ToString();
                    var substrings = curItem.Split(',');
                    var id = Int32.Parse(substrings[0]);
                    var json = HostJsonApiObjectFactory.CreateUnpairFromPeripheralRequest(id);
                    SendToWebsocket(json.Serialize());
                }
            }
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (_ws.IsAlive)
            {
                _ws.CloseAsync();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SendPeripheralRequest();
        }
    }
}
