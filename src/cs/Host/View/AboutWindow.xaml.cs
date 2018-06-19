using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;

namespace JJA.Anperi.Host.View
{
    /// <summary>
    /// Interaktionslogik für AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
        }

        private void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Link_Click(object sender, RoutedEventArgs e)
        {
            if (sender.GetType() != typeof(Hyperlink))
            {
                return;
            }

            var link = (Hyperlink) sender;
            Process.Start(link.NavigateUri.ToString());
        }
    }
}
