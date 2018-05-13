using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using AnperiRemote.Annotations;
using AnperiRemote.Model;

namespace AnperiRemote.ViewModel
{
    class MainWindowViewModel : MainWindowDesignerViewModel, INotifyPropertyChanged
    {
        private Brush _ipcStatusBrush = Brushes.DarkMagenta;

        public MainWindowViewModel()
        {
            AnperiModel.Instance.PropertyChanged += AnperiModel_PropertyChanged;
            VolumeModel.Instance.PropertyChanged += VolumeModel_PropertyChanged1;
        }

        private void VolumeModel_PropertyChanged1(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(VolumeModel.Volume):
                    OnPropertyChanged(nameof(Volume));
                    OnPropertyChanged(nameof(VolumeText));
                    break;
            }
        }

        private void AnperiModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(AnperiModel.HasControl):
                    OnPropertyChanged(nameof(HasControl));
                    CalcStatusBrush();
                    break;
                case nameof(AnperiModel.IsPeripheralConnected):
                    OnPropertyChanged(nameof(HasPeripheral));
                    CalcStatusBrush();
                    break;
                case nameof(AnperiModel.IsConnected):
                    OnPropertyChanged(nameof(AnperiConnectionStatus));
                    CalcStatusBrush();
                    break;
            }
        }

        public override SettingsDesignerViewModel SettingsViewModel { get; } = new SettingsViewModel();

        private void CalcStatusBrush()
        {
            Brush newBrush;
            if (!AnperiModel.Instance.IsConnected)
            {
                newBrush = Brushes.Red;
            }
            else
            {
                if (AnperiModel.Instance.HasControl && AnperiModel.Instance.IsPeripheralConnected)
                {
                    newBrush = Brushes.LimeGreen;
                }
                else
                {
                    newBrush = Brushes.Yellow;
                }
            }
            if (!newBrush.Equals(_ipcStatusBrush))
            {
                _ipcStatusBrush = newBrush;
                OnPropertyChanged(nameof(IpcStatusBrush));
            }
        }

        public override string VolumeText => VolumeModel.Instance.Volume.ToString() + "%";

        public override int Volume
        {
            get => VolumeModel.Instance.Volume;
            set => VolumeModel.Instance.Volume = value;
        }

        public override Brush IpcStatusBrush => _ipcStatusBrush;

        public override string AnperiConnectionStatus => AnperiModel.Instance.IsConnected ? "Connected to Host." : "Not connected to Host.";

        public override string HasPeripheral => AnperiModel.Instance.IsPeripheralConnected
            ? "Peripheral is connected."
            : "No peripheral is connected.";

        public override string HasControl =>
            AnperiModel.Instance.HasControl ? "We do have control." : "No control for us.";

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
