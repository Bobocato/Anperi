using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using JJA.Anperi.WpfUtility;

namespace JJA.Anperi.Host.Model
{
    [Serializable]
    public class HostDataModel : INotifyPropertyChanged
    {
        private bool _tray = false;
        private bool _autostart = false;
        private string _token = "";
        private int _favorite = -1;

        public bool Tray
        {
            get => _tray;
            set
            {
                if (_tray == value) return;
                _tray = value;
                OnPropertyChanged();
            }
        }

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