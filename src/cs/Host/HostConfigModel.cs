using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using JJA.Anperi.Host.Utility;
using JJA.Anperi.Utility;

namespace JJA.Anperi.Host
{
    class HostConfigModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private HostDataModel _dataModel;

        public HostConfigModel()
        {
            _dataModel = ConfigHandler.Load();
            _dataModel.PropertyChanged += OnModelPropertyChanged;
        }

        private void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e.PropertyName);
        }

        public HostDataModel DataModel
        {
            get => _dataModel;
            set
            {
                _dataModel = value;
                OnPropertyChanged();
                ConfigHandler.Save(_dataModel);
                //TODO: if autostart, do sth
            }
        }

        protected virtual void OnPropertyChanged(
            [CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}
