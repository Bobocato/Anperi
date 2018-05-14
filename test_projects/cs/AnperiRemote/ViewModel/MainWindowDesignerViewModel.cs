using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using AnperiRemote.Model;

namespace AnperiRemote.ViewModel
{
    class MainWindowDesignerViewModel
    {
        public MainWindowDesignerViewModel()
        {
            //check if it is a designer instance
            if (this.GetType() != typeof(MainWindowDesignerViewModel)) return;

            SettingsViewModel = new SettingsDesignerViewModel();
        }

        public virtual SettingsDesignerViewModel SettingsViewModel { get; }

        public virtual Brush IpcStatusBrush => Brushes.DarkMagenta;

        public virtual string AnperiConnectionStatus => "Anperi connection status.";

        public virtual string HasPeripheral => "Designers dont have peripherals.";

        public virtual string HasControl => "The designer also doesnt have control.";

        public virtual string VolumeText => "50";
        public virtual int Volume { get; set; } = 50;
    }
}
