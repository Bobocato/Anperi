using System.Windows;
using JJA.Anperi.Host.Model;
using JJA.Anperi.Host.Utility;
using JJA.Anperi.Host.ViewModel;

namespace JJA.Anperi.Host.View
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
            SpawnPopup(Popup.WindowType.Pair);
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
            SpawnPopup(Popup.WindowType.Rename);
        }

        private void ButDisconnect_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Disconnect();
        }

        private void ButSendMessage_Click(object sender, RoutedEventArgs e)
        {
            SpawnPopup(Popup.WindowType.Message);
        }

        private void ButOptions_Click(object sender, RoutedEventArgs e)
        {
            var win = new SettingsWindow();
            if (win.ShowDialog().GetValueOrDefault(false))
            {
                win.Settings.SaveToDataModel(ConfigHandler.Load());
            }
        }

        private void SpawnPopup(Popup.WindowType windowType)
        {
            var popup = new Popup(windowType);
            if (windowType == Popup.WindowType.Rename)
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
