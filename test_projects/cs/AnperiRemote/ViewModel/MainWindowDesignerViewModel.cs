using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AnperiRemote.Model;

namespace AnperiRemote.ViewModel
{
    class MainWindowDesignerViewModel
    {
        public virtual SettingsModel SettingsModel { get; set; } = new SettingsModel();
    }
}
