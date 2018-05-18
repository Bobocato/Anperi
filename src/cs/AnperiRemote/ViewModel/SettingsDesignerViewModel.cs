using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using AnperiRemote.Model;
using AnperiRemote.Utility;
using JJA.Anperi.WpfUtility;

namespace AnperiRemote.ViewModel
{
    class SettingsDesignerViewModel
    {
        public SettingsDesignerViewModel()
        {
            //check if it is a designer instance
            if (this.GetType() != typeof(SettingsDesignerViewModel)) return;

            Model = new SettingsModel();
            AutostartChangedCommand = new WpfUtil.RelayCommand((o) => {});
        }

        public virtual SettingsModel Model { get; }

        public virtual ICommand AutostartChangedCommand { get; }
    }
}
