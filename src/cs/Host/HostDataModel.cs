using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace JJA.Anperi.Host
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