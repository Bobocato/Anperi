using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
            var viewModel = (HostViewModel) this.DataContext;
            var windowType = viewModel.PopupTitle;

            switch (windowType)
            {
                case "pair":
                    viewModel.Pair(PairCode.Text);
                    break;
                case "options":
                    //TODO: implement this
                    break;
                case "rename":
                    viewModel.Rename(PeriId, NewName.Text);
                    break;
                case "message":
                    viewModel.SendMessage(MessageBox.Text);
                    break;
                default:
                    throw new NotImplementedException();
            }
            this.Close();
        }

        private void ButCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
