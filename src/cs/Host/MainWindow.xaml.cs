using System;
using System.ComponentModel;
using System.Windows;
using JJA.Anperi.Internal.Api.Host;

namespace JJA.Anperi.Host
{
    public partial class MainWindow
    {
        private readonly HostViewModel _viewModel;

        public MainWindow()
        {
            //TODO: rework UI
            _viewModel = new HostViewModel(Dispatcher);
            DataContext = _viewModel;
            InitializeComponent();
        }

        private void ButPair_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.PopupTitle = "pair";
            var popup = new Popup();
            popup.DataContext = this.DataContext;

            popup.Show();
            //_viewModel.Pair(CodeBox.Text);
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
            _viewModel.PopupTitle = "rename";
            var popup = new Popup();
            popup.DataContext = this.DataContext;
            var item = (HostJsonApiObjectFactory.ApiPeripheral) PeriBox.SelectedItem;
            popup.PeriId = item.id;

            popup.Show();
        }

        private void ButDisconnect_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Disconnect();
        }

        private void ButSendMessage_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.PopupTitle = "message";
            var popup = new Popup();
            popup.DataContext = this.DataContext;

            popup.Show();
            //_viewModel.SendMessage(MessageBox.Text);
        }


    }
}
