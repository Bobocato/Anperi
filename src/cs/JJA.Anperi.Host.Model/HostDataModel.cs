using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using JJA.Anperi.WpfUtility;

namespace JJA.Anperi.Host.Model
{
    [Serializable]
    public class HostDataModel : INotifyPropertyChanged
    {
        private bool _autostart = false;
        private string _token = "";
        private string _serverAddress = "ws://localhost:5000/api/ws";
        private int _favorite = -1;

        public int Favorite
        {
            get => _favorite;
            set
            {
                _favorite = value;
                OnPropertyChanged();
            }
        }

        public bool Autostart
        {
            get => _autostart;
            set
            {
                if (_autostart == value) return;
                _autostart = value;
                if (_autostart) WpfUtil.AddToAutostart("-tray");
                else WpfUtil.RemoveFromAutostart();
                OnPropertyChanged();
            }
        }

        public string Token
        {
            get => _token;
            set
            {
                if (_token == value) return;
                _token = value;
                OnPropertyChanged();
            }
        }

        public string ServerAddress
        {
            get => _serverAddress;
            set
            {
                if (_serverAddress == value) return;
                _serverAddress = value;
                OnPropertyChanged();
            }
        }

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(
            [CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}