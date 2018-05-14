using System;
using System.Windows;

namespace JJA.Anperi.Host
{
    /// <summary>
    /// Interaktionslogik für Popup.xaml
    /// </summary>
    public partial class Popup : Window
    {
        public Popup()
        {
            InitializeComponent();
        }

        public int PeriId { get; set; } = -1;

        private void ButOkay_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = (HostViewModel)DataContext;

            switch (viewModel.PopupTitle)
            {
                case "pair":
                    viewModel.Pair(InputBox.Text);
                    break;
                case "options":
                    //TODO: implement this
                    break;
                case "rename":
                    viewModel.Rename(PeriId, InputBox.Text);
                    break;
                case "message":
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
