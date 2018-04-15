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
using JJA.Anperi.HostApi;

namespace JJA.Anperi.Host
{
    public partial class MainWindow : Window
    {
        private string _wsAddress = "ws://localhost:5000/api/ws";
        private WebSocket _ws;

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
                    };

                    _ws.OnMessage += (sender, e) =>
                    {                       
                        var json = JsonApiObject.Deserialize(e.Data);
                        var context = json.context.ToString();

                        if (context.Equals("server"))
                        {
                            switch (json.message_code)
                            {
                                case "pair":
                                    var success = json.data["success"];
                                    if (success)
                                    {
                                        ShowMessage("Successful paired!");
                                    }else
                                    {
                                        ShowMessage("Something went wrong while pairing!");
                                    }
                                    break;
                                case "get_available_peripherals":

                                    break;
                                case "connect_to_peripheral":

                                    break;
                                case "disconnect_ from _peripheral":

                                    break;
                                case "unpair":

                                    break;
                            }
                        }else if (context.Equals("device"))
                        {
                            
                        }
                        else
                        {
                            ShowMessage("Unknown context!");
                        }
                    };

                    _ws.OnClose += (sender, e) =>
                    {
                        InfoBlock.Text = "No current WebSocket connection";
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

        private void ButPair_Click(object sender, RoutedEventArgs e)
        {
            string pairCode = CodeBox.Text;
            var json =
                HostJsonApiObjectFactory.CreatePairingRequest(pairCode);
            _ws.Send(json.Serialize());
        }

        private void ButConnect_Click(object sender, RoutedEventArgs e)
        {
            //TODO: send a JSON Object for connecting to a device
        }

        private void ButSendMessage_Click(object sender, RoutedEventArgs e)
        {
            string message = MessageBox.Text;
            //TODO: send a JSON Object for messaging the connected device
        }

        private void ButDisconnect_Click(object sender, RoutedEventArgs e)
        {
            var json = HostJsonApiObjectFactory
                .CreateDisconnectFromPeripheralRequest();
            _ws.Send(json.Serialize());
        }

        private void ButUnpair_Click(object sender, RoutedEventArgs e)
        {
            //TODO: send a JSON object for unpairing with choosen device
        }
    }
}
