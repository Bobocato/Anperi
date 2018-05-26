using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Threading;
using JJA.Anperi.Host.Model;

namespace JJA.Anperi.Host.ViewModel
{
    //TODO: probably split model into multiple classes because this will get REALLY messy the moment IPC comes into play
    class HostViewModel : INotifyPropertyChanged
    {
        private readonly Dispatcher _dispatcher;
        public event PropertyChangedEventHandler PropertyChanged;
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

        public bool Tray
        {
            get => _model.Tray;
            set
            {
                _model.Tray = value;
                OnPropertyChanged(nameof(Tray));
            }
        }

        public bool Autostart
        {
            get => _model.Autostart;
            set
            {
                _model.Autostart = value;
                OnPropertyChanged(nameof(Autostart));
            }
        }

        public string Info1
        {
            get => _model.Info1;
            set
            {
                _model.Info1 = value;
                OnPropertyChanged(nameof(Info1));
            }
        }

        public string Info2
        {
            get => _model.Info2;
            set
            {
                _model.Info2 = value;
                OnPropertyChanged(nameof(Info2));
            }
        }

        public string Info3
        {
            get => _model.Info3;
            set
            {
                _model.Info3 = value;
                OnPropertyChanged(nameof(Info3));
            }
        }

        public string ConnectedTo => _model.ConnectedPeripheral?.Name ?? "";

        public string PopupTitle
        {
            get => _model.PopupTitle;
            set
            {
                _model.PopupTitle = value;
                OnPropertyChanged(nameof(PopupTitle));
            }
        }

        public bool PopupInput => _model.PopupInput;

        public bool PopupOptions => _model.PopupOptions;

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
            _model.Favorite = ((Peripheral) item).Id;
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

        private void OnModelPropertyChanged(object sender,
            PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(HostModel.Peripherals):
                    RefillPeripherals();
                    break;
                case nameof(HostModel.ConnectedPeripheral):
                    OnPropertyChanged(nameof(ButDisconnectVisible));
                    OnPropertyChanged(nameof(ConnectedTo));
                    break;
                default:
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

        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

    }
}
