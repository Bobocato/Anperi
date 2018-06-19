using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using JJA.Anperi.Host.Model.Annotations;
using JJA.Anperi.Internal.Api.Host;

namespace JJA.Anperi.Host.Model
{
    public class Peripheral : INotifyPropertyChanged
    {
        private readonly HostJsonApiObjectFactory.ApiPeripheral _peripheral;
        private bool _isFavorite = false;
        private bool _isConnected = false;
        private Dictionary<string, dynamic> _peripheralInfo;

        public Dictionary<string, dynamic> PeripheralInfo
        {
            get { return _peripheralInfo; }
            set
            {
                _peripheralInfo = value;
                OnPropertyChanged();
            }
        }

        internal Peripheral(HostJsonApiObjectFactory.ApiPeripheral peripheral)
        {
            _peripheral = peripheral ?? throw new ArgumentException("Peripheral cannot be null.", nameof(peripheral));
        }

        internal Peripheral(int id, string name, bool online)
        {
            if (name == null) throw new ArgumentException("A device must have a name", nameof(name));
            _peripheral = new HostJsonApiObjectFactory.ApiPeripheral {id = id, name = name, online = online};
        }

        public int Id => _peripheral.id;
        public string Name
        {
            get => _peripheral.name;
            set
            {
                if (value == _peripheral.name) return;
                _peripheral.name = value;
                OnPropertyChanged();
            }
        }

        public bool Online
        {
            get => _peripheral.online;
            set
            {
                if (value == _peripheral.online) return;
                _peripheral.online = value;
                OnPropertyChanged();
            }
        }

        public bool IsFavorite
        {
            get => _isFavorite;
            set
            {
                if (value == _isFavorite) return;
                _isFavorite = value;
                OnPropertyChanged();
            }
        }

        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                if (value == _isConnected) return;
                _isConnected = value;
                OnPropertyChanged();
            }
        }

        public override bool Equals(object obj)
        {
            return _peripheral.Equals(obj);
        }

        public override int GetHashCode()
        {
            return _peripheral.GetHashCode();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
