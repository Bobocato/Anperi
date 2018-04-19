using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using JJA.Anperi.HostApi;

namespace JJA.Anperi.Host
{
    public partial class MainWindow : Window
    {
        private readonly HostViewModel _viewModel;

        public MainWindow()
        {
            _viewModel = new HostViewModel(Dispatcher);
            this.DataContext = _viewModel;
            InitializeComponent();
        }

        private void ButPair_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Pair(CodeBox.Text);
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            _viewModel.Close();
        }

        private void ButUnpair_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Unpair(PeriBox.SelectedItem);
        }

        private void ButConnect_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Connect(PeriBox.SelectedItem);
        }

        private void ButDisconnect_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Disconnect();
        }

        private void ButSendMessage_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.SendMessage(MessageBox.Text);
        }


    }
}
