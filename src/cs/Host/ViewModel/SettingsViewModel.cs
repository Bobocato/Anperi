using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using JJA.Anperi.Host.Annotations;
using JJA.Anperi.Host.Model;
using JJA.Anperi.Host.Utility;

namespace JJA.Anperi.Host.ViewModel
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        public SettingsViewModel()
        {
            HostDataModel mdl = ConfigHandler.Load();
            _autostart = mdl.Autostart;
        }

        private bool _autostart;

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

        public void SaveToDataModel(HostDataModel dataModel)
        {
            dataModel.Autostart = _autostart;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
