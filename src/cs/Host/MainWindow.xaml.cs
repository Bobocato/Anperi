﻿using System.ComponentModel;
using System.Windows;
using JJA.Anperi.Host.Model;
using JJA.Anperi.Internal.Api.Host;

namespace JJA.Anperi.Host
{
    public partial class MainWindow
    {
        private readonly HostViewModel _viewModel;

        public MainWindow()
        {
            _viewModel = new HostViewModel(Dispatcher);
            DataContext = _viewModel;
            InitializeComponent();
        }

        private void ButPair_Click(object sender, RoutedEventArgs e)
        {
            SpawnPopup("pair");
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

        private void ButFavorite_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Favorite(PeriBox.SelectedItem);
        }

        private void ButRename_Click(object sender, RoutedEventArgs e)
        {
            SpawnPopup("rename");
        }

        private void ButDisconnect_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Disconnect();
        }

        private void ButSendMessage_Click(object sender, RoutedEventArgs e)
        {
            SpawnPopup("message");
        }

        private void ButOptions_Click(object sender, RoutedEventArgs e)
        {
            SpawnPopup("options");
        }

        private void SpawnPopup(string windowType)
        {
            _viewModel.PopupTitle = windowType;
            var popup = new Popup();
            if (windowType == "rename")
            {
                var item = (Peripheral)PeriBox.SelectedItem;
                popup.PeriId = item.Id;
            }
            popup.DataContext = this.DataContext;
            popup.WindowStartupLocation = WindowStartupLocation.Manual;
            popup.Left = this.Left + (this.Width / 2);
            popup.Top = this.Top + (this.Height - popup.Height) / 2;
            popup.ShowDialog();
        }
    }
}
