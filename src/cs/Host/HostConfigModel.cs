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
using JJA.Anperi.Utility;

namespace JJA.Anperi.Host
{
    class HostConfigModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _configPath = "settings.data";
        private string _saltPath = "salt.data";
        private byte[] _salt;
        private HostDataModel _dataModel;

        public HostConfigModel()
        {
            if (File.Exists(_saltPath))
            {
                var lines = File.ReadAllLines(_saltPath);
                if (lines.Length != 0)
                {
                    _salt = Encoding.ASCII.GetBytes(lines[0]);
                }              
            }

            _dataModel = Load(_configPath);
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
                Save();
                //TODO: if autostart, do sth
            }
        }

        public void ToTray()
        {
            //TODO: reduce to tray here
        }

        public byte[] Protect(byte[] data)
        {
            //TODO: Salt = random Byte[], saved in config
            return ProtectedData.Protect(data, _salt, DataProtectionScope.LocalMachine);
        }

        public byte[] Unprotect(byte[] data)
        {
            //TODO: look protect
            return ProtectedData.Unprotect(data, _salt, DataProtectionScope.LocalMachine);
        }

        protected virtual void OnPropertyChanged(
            [CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this,
                new PropertyChangedEventArgs(propertyName));
        }

        private HostDataModel Load(string path)
        {
            bool fileExists = File.Exists(path);
            if (!fileExists) return new HostDataModel();

            HostDataModel model = null;
            try
            {
                byte[] file = File.ReadAllBytes(path);
                file = Unprotect(file);
                using (var ms = new MemoryStream(file))
                {
                    var bf = new BinaryFormatter();
                    model = (HostDataModel)bf.Deserialize(ms);
                }
            }
            catch (Exception e)
            {
                Util.TraceException("Error loading HostData, restoring defaults ...", e);
            }
            return model ?? new HostDataModel();
        }

        public void Save()
        {
            try
            {
                byte[] data;
                CreateNewSalt();
                using (var ms = new MemoryStream())
                {
                    var bf = new BinaryFormatter();
                    bf.Serialize(ms, this.DataModel);
                    data = ms.ToArray();
                }
                data = Protect(data);
                File.WriteAllBytes(_configPath, data);
            }
            catch (Exception e)
            {
                Util.TraceException("Error while saving VpnData", e);
            }
        }

        private void CreateNewSalt()
        {
            var salt = new byte[8];
            var rnd = new Random();
            rnd.NextBytes(salt);
            _salt = salt;
            var writer = new StreamWriter(_saltPath, false);
            writer.WriteLine(Encoding.ASCII.GetString(_salt));
            writer.Close();
        }
    }
}
