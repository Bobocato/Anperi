using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;
using JJA.Anperi.Host.Annotations;
using JJA.Anperi.Host.Model;

namespace JJA.Anperi.Host.ViewModel
{
    //TODO: probably split model into multiple classes because this will get REALLY messy the moment IPC comes into play
    class HostViewModel : INotifyPropertyChanged
    {
        private readonly Dispatcher _dispatcher;
        private readonly HostModel _model;

        public HostViewModel(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            _model = HostModel.Instance;
            Peripherals = new ObservableCollection<Peripheral>(_model.Peripherals);
            _model.PropertyChanged += OnModelPropertyChanged;
        }       

        public bool ButDisconnectVisible
        {
            get => _model.ConnectedPeripheral != null;
        }

        public string Message
        {
            get => _model.Message;
        }

        public string ConnectedString
        {
            get
            {
                if (IsConnected) return $"Logged in to {_model.ServerAddress} as {_model.OwnName}";
                return $"Trying to connect to: {_model.ServerAddress}";
            }
        }

        public bool IsConnected => _model.IsConnected;
        public bool IsConnecting => !_model.IsConnected;

        public string ConnectedTo => _model.ConnectedPeripheral?.Name ?? "";

        public ObservableCollection<Peripheral> Peripherals { get; }

        public void Close()
        {
            _model.Close();
        }

        public void Pair(string pairCode)
        {
            _model.Pair(pairCode);
        }

        public void Unpair(object item)
        {
            _model.Unpair(item);
        }

        public void Favorite(object item)
        {
            _model.Favorite = ((Peripheral) item)?.Id ?? -1;
        }

        public void Rename(int id, string name)
        {
            _model.Rename(id, name);
        }

        public void Connect(object item)
        {
            _model.Connect((Peripheral)item);
        }

        public void Disconnect()
        {
            _model.Disconnect();
        }

        public void SendMessage(string msg)
        {
            _model.SendMessage(msg);
        }

        public static Visibility IsDebug
        {
#if DEBUG
            get => Visibility.Visible;
#else
            get => Visibility.Collapsed;
#endif
        }

        public bool ArePeripheralsLoading => !_model.IsPeripheralListLoaded;

        private void OnModelPropertyChanged(object sender,
            PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(HostModel.Favorite):
                case nameof(HostModel.Peripherals):
                    RefillPeripherals();
                    break;
                case nameof(HostModel.ConnectedPeripheral):
                    OnPropertyChanged(nameof(ButDisconnectVisible));
                    OnPropertyChanged(nameof(ConnectedTo));
                    break;
                case nameof(HostModel.ServerAddress):
                case nameof(HostModel.OwnName):
                    OnPropertyChanged(nameof(HostModel.OwnName));
                    OnPropertyChanged(nameof(ConnectedString));
                    break;
                case nameof(HostModel.IsPeripheralListLoaded):
                    OnPropertyChanged(nameof(ArePeripheralsLoading));
                    break;
                case nameof(HostModel.IsConnected):
                    OnPropertyChanged(nameof(IsConnected));
                    OnPropertyChanged(nameof(IsConnecting));
                    break;
                default:
                    if (GetType().GetProperty(e.PropertyName) != null)
                        OnPropertyChanged(e.PropertyName);
                    break;
            }
        }

        private void RefillPeripherals()
        {
            _dispatcher.Invoke(() =>
            {
                Peripherals.Clear();
                _model.Peripherals.ForEach((a) =>
                {
                    Peripherals.Add(a);
                });
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
