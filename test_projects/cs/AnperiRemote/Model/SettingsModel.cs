using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using AnperiRemote.Annotations;
using AnperiRemote.DAL;
using Newtonsoft.Json;

namespace AnperiRemote.Model
{
    class SettingsModel : INotifyPropertyChanged
    {
        [NonSerialized]
        private bool _autostartEnabled = false;
        [NonSerialized]
        private bool _shutdownControlEnabled = true;
        [NonSerialized]
        private bool _volumeControlEnabled = true;
        
        [JsonIgnore]
        public static SettingsModel Instance => SettingsFile.Instance.SettingsModel;

        public void Save()
        {
            SettingsFile.Instance.Save();
        }

        public bool AutostartEnabled
        {
            get => _autostartEnabled;
            set
            {
                if (value == _autostartEnabled) return;
                _autostartEnabled = value;
                OnPropertyChanged();
            }
        }

        public bool ShutdownControlEnabled
        {
            get => _shutdownControlEnabled;
            set
            {
                if (value == _shutdownControlEnabled) return;
                _shutdownControlEnabled = value;
                OnPropertyChanged();
            }
        }

        public bool VolumeControlEnabled
        {
            get => _volumeControlEnabled;
            set
            {
                if (value == _volumeControlEnabled) return;
                _volumeControlEnabled = value;
                OnPropertyChanged();
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
