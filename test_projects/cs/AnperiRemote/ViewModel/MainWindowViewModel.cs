using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using AnperiRemote.Annotations;
using AnperiRemote.Model;

namespace AnperiRemote.ViewModel
{
    class MainWindowViewModel : MainWindowDesignerViewModel, INotifyPropertyChanged
    {
        public override SettingsModel SettingsModel => SettingsModel.Instance;

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
