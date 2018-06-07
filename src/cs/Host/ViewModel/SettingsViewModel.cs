using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using JJA.Anperi.Host.Annotations;
using JJA.Anperi.Host.Model;
using JJA.Anperi.Host.Model.Utility;
using JJA.Anperi.Host.Utility;
using JJA.Anperi.WpfUtility;

namespace JJA.Anperi.Host.ViewModel
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        public SettingsViewModel()
        {
            HostDataModel mdl = ConfigHandler.Load();
            HostModel.Instance.PropertyChanged += Instance_PropertyChanged;
            _autostart = mdl.Autostart;
            _serverAddress = mdl.ServerAddress;
            _ownName = HostModel.Instance.OwnName;

            CommandResetServer = new WpfUtil.RelayCommand((o) => ServerAddress = HostModel.DefaultServer);
        }

        private void Instance_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(HostModel.IsConnected))
            {
                OnPropertyChanged(nameof(IsConnected));
            }
        }

        private bool _autostart;
        private string _serverAddress;
        private string _ownName;

        public bool Autostart
        {
            get { return _autostart; }
            set
            {
                if (value == _autostart) return;
                _autostart = value;
                OnPropertyChanged();
            }
        }

        public string ServerAddress
        {
            get { return _serverAddress; }
            set
            {
                if (value == _serverAddress) return;
                _serverAddress = value;
                OnPropertyChanged();
            }
        }

        public string OwnName
        {
            get { return _ownName; }
            set
            {
                if (value == _ownName) return;
                _ownName = value;
                OnPropertyChanged();
            }
        }

        public ICommand CommandResetServer { get; }

        public bool IsConnected => HostModel.Instance.IsConnected;

        public void SaveToDataModel(HostDataModel dataModel)
        {
            dataModel.Autostart = _autostart;
            dataModel.ServerAddress = _serverAddress;
            if (_ownName != HostModel.Instance.OwnName) HostModel.Instance.RenameSelf(_ownName);
            ConfigHandler.Save();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
