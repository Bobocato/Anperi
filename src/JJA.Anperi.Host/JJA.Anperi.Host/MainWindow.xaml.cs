using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
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

namespace JJA.Anperi.Host
{
    public partial class MainWindow : Window
    {
        private string _wsAddress = "ws://localhost:5000/api/ws";
        private ClientWebSocket ws;

        public MainWindow()
        {
            InitializeComponent();
            InitializeWebSocket();
        }

        private void InitializeWebSocket()
        {
            InfoBlock.Text = "Trying to connect to: " + _wsAddress;
            //TODO: Initialize the WebSocket connection
            /*var uri = new Uri(_wsAddress);
            ws.ConnectAsync(uri, CancellationToken.None);*/
            InfoBlock.Text = "Current WebSocket-Address is: " + _wsAddress;
        }

        private void ButPair_Click(object sender, RoutedEventArgs e)
        {
            string pairCode = CodeBox.Text;
            //TODO: send a JSON Object for pairing with a device
            
        }

        private void ButConnect_Click(object sender, RoutedEventArgs e)
        {
            //TODO: send a JSON Object for connecting to a device
        }

        private void ButChangeWS_Click(object sender, RoutedEventArgs e)
        {
            _wsAddress = WsBox.Text;
            InitializeWebSocket();
        }

        private void ButSendMessage_Click(object sender, RoutedEventArgs e)
        {
            string message = MessageBox.Text;
            //TODO: send a JSON Object for messaging the connected device
        }
    }
}
