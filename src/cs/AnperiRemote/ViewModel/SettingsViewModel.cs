using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Navigation;
using AnperiRemote.Model;
using AnperiRemote.Utility;
using JJA.Anperi.WpfUtility;

namespace AnperiRemote.ViewModel
{
    class SettingsViewModel : SettingsDesignerViewModel
    {
        public override SettingsModel Model => SettingsModel.Instance;

        public override ICommand AutostartChangedCommand { get; } = new WpfUtil.RelayCommand((o) =>
        {
            if (!(o is bool)) throw new ArgumentException("This command requires a bool parameter.");
            bool param = (bool) o;
            if (param) WpfUtil.AddToAutostart("-trayStart");
            else WpfUtil.RemoveFromAutostart();
        });
    }
}
