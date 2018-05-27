using System;
using System.Windows;
using JJA.Anperi.Host.ViewModel;

namespace JJA.Anperi.Host
{
    /// <summary>
    /// Interaktionslogik für Popup.xaml
    /// </summary>
    public partial class Popup : Window
    {
        public enum WindowType
        {
            Rename,
            Pair,
            Message
        }

        private WindowType _windowType = WindowType.Message;

        public Popup()
        {
            InitializeComponent();
        }

        public Popup(WindowType windowType)
        {
            InitializeComponent();
            _windowType = windowType;
            switch (windowType)
            {
                case WindowType.Rename:
                    Title.Text = "Rename";
                    break;
                case WindowType.Pair:
                    Title.Text = "Pair";
                    break;
                case WindowType.Message:
                    Title.Text = "Message";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(windowType), windowType, null);
            }
        }

        public int PeriId { get; set; } = -1;

        private void ButOkay_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = (HostViewModel)DataContext;

            switch (_windowType)
            {
                case WindowType.Pair:
                    viewModel.Pair(InputBox.Text);
                    break;
                case WindowType.Rename:
                    viewModel.Rename(PeriId, InputBox.Text);
                    break;
                case WindowType.Message:
                    viewModel.SendMessage(InputBox.Text);
                    break;
                default:
                    throw new NotImplementedException();
            }
            Close();
        }

        private void ButCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
