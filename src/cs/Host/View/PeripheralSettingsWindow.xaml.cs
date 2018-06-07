using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using JJA.Anperi.Host.Annotations;
using JJA.Anperi.Host.Model;
using MessageBox = System.Windows.Forms.MessageBox;

namespace JJA.Anperi.Host.View
{
    /// <summary>
    /// Interaction logic for PeripheralSettingsWindow.xaml
    /// </summary>
    public partial class PeripheralSettingsWindow : Window, INotifyPropertyChanged
    {
        private string _name;

        public PeripheralSettingsWindow(Peripheral peripheral)
        {
            InitializeComponent();
            DataContext = this;
            Peripheral = peripheral;
            NewName = peripheral.Name;
        }

        public Peripheral Peripheral { get; }

        public bool UnpairRequested { get; private set; } = false;

        public string NewName
        {
            get => _name;
            set
            {
                if (value == _name) return;
                _name = value;
                OnPropertyChanged();
            }
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ButtonUnpair_Click(object sender, RoutedEventArgs e)
        {
            DialogResult dr = MessageBox.Show($"Do you really want to remove the peripheral {Peripheral.Name}?", "Unpairing Peripheral",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (dr == System.Windows.Forms.DialogResult.Yes)
            {
                UnpairRequested = true;
                DialogResult = true;
                Close();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
