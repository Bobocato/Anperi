using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using JJA.Anperi.Host.Annotations;
using JJA.Anperi.Host.ViewModel;

namespace JJA.Anperi.Host.View
{
    /// <summary>
    /// Interaktionslogik für StringDialog.xaml
    /// </summary>
    public partial class StringDialog : Window, INotifyPropertyChanged
    {
        private string _message = "CHANGE THIS PLS!";
        private string _result = "";

        public StringDialog()
        {
            InitializeComponent();
        }

        public StringDialog(string title, string message, string initialValue = "") : this()
        {
            base.Title = title;
            Message = message;
            Result = initialValue;
        }

        public string Result
        {
            get => _result;
            set
            {
                if (value == _result) return;
                _result = value;
                OnPropertyChanged();
            }
        }

        public string Message
        {
            get => _message;
            set
            {
                if (value == _message) return;
                _message = value;
                OnPropertyChanged();
            }
        }

        private void ButOkay_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void ButCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            switch (e.Key)
            {
                case Key.Return:
                    ButOkay_Click(null, null);
                    break;
                case Key.Escape:
                    ButCancel_Click(null, null);
                    break;
                default:
                    e.Handled = false;
                    break;
            }
        }
    }
}
