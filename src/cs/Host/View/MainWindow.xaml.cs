using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using JJA.Anperi.Host.Model;
using JJA.Anperi.Host.Model.Utility;
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
            StringDialog wndPair = new StringDialog("Pairing", "Enter the code you can see on the device and click ok.");
            if (wndPair.ShowDialog().GetValueOrDefault(false))
            {
                _viewModel.Pair(wndPair.Result);
            }
        }

        private void ButUnpair_Click(object sender, RoutedEventArgs e)
        {
            Peripheral p = (Peripheral)((FrameworkElement)sender).DataContext;
            _viewModel.Unpair(p);
        }

        private void ButConnect_Click(object sender, RoutedEventArgs e)
        {
            Peripheral p = (Peripheral) ((FrameworkElement)sender).DataContext;
            if (p.IsConnected)
            {
                _viewModel.Disconnect();
            }
            else
            {
                _viewModel.Connect(p);
            }
        }

        private void ButFavorite_Click(object sender, RoutedEventArgs e)
        {
            Peripheral p = (Peripheral)((FrameworkElement)sender).DataContext;
            _viewModel.Favorite(p.IsFavorite ? null : p);
        }

        private void ButRename_Click(object sender, RoutedEventArgs e)
        {
            Peripheral p = (Peripheral)((FrameworkElement)sender).DataContext;
            StringDialog wndPair = new StringDialog("Renaming", "Enter a new name for the device");
            if (wndPair.ShowDialog().GetValueOrDefault(false))
            {
                if (!string.IsNullOrWhiteSpace(wndPair.Result)) _viewModel.Rename(p.Id, wndPair.Result);
            }
        }

        private void ButSendMessage_Click(object sender, RoutedEventArgs e)
        {
            StringDialog wndPair =
                new StringDialog("Debug Message", "Enter the message you want to send to the peripheral.");
            if (wndPair.ShowDialog().GetValueOrDefault(false))
            {
                _viewModel.SendMessage(wndPair.Result);
            }
        }

        private void ButOptions_Click(object sender, RoutedEventArgs e)
        {
            var win = new SettingsWindow();
            if (win.ShowDialog().GetValueOrDefault(false))
            {
                win.Settings.SaveToDataModel(ConfigHandler.Load());
            }
        }

        private void ButAbout_Click(object sender, RoutedEventArgs e)
        {
            throw new System.NotImplementedException();
        }

        private void ButRestart_Click(object sender, RoutedEventArgs e)
        {
            throw new System.NotImplementedException();
        }

        private void ButExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void ButDeviceSettings_Click(object sender, RoutedEventArgs e)
        {
            Peripheral p = (Peripheral)((FrameworkElement)sender).DataContext;
            StringDialog wndPair = new StringDialog("Renaming", "Enter a name for the device", p.Name);
            if (wndPair.ShowDialog().GetValueOrDefault(false))
            {
                if (!string.IsNullOrWhiteSpace(wndPair.Result)) _viewModel.Rename(p.Id, wndPair.Result);
            }
        }
    }
}
