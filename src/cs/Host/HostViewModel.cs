using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows.Threading;
using JJA.Anperi.Internal.Api.Host;

namespace JJA.Anperi.Host
{
    //TODO: seperate DLL for the model
    //TODO: probably split model into multiple classes because this will get REALLY messy the moment IPC comes into play
    class HostViewModel : INotifyPropertyChanged
    {
        private readonly Dispatcher _dispatcher;
        public event PropertyChangedEventHandler PropertyChanged;
        private readonly HostModel _model;
        private readonly HostConfigModel _configModel;
        private ObservableCollection<HostJsonApiObjectFactory.ApiPeripheral> _peripherals;

        public HostViewModel(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            _configModel = new HostConfigModel();
            _configModel.PropertyChanged += OnModelPropertyChanged;
            _model = new HostModel(_configModel.DataModel.Token);
            _model.PropertyChanged += OnModelPropertyChanged;
        }       

        public bool ButDisconnect
        {
            get => _model.ButDisconnect;
            set
            {
                _model.ButDisconnect = value;
                OnPropertyChanged(nameof(ButDisconnect));
            }
        }

        public bool Tray
        {
            get => _configModel.DataModel.Tray;
            set
            {
                _configModel.DataModel.Tray = value;
                OnPropertyChanged(nameof(Tray));
            }
        }

        public bool Autostart
        {
            get => _configModel.DataModel.Autostart;
            set
            {
                _configModel.DataModel.Autostart = value;
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

        public string ConnectedTo
        {
            get => _model.ConnectedTo;
            set
            {
                _model.ConnectedTo = value;
                OnPropertyChanged(nameof(ConnectedTo));
            }
        }

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

        public ObservableCollection<HostJsonApiObjectFactory.ApiPeripheral> Peripherals => _peripherals;

        public void Close()
        {
            _configModel.Save();
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
            _model.Favorite = ((HostJsonApiObjectFactory.ApiPeripheral) item).id;
        }

        public void Rename(int id, string name)
        {
            _model.Rename(id, name);
        }

        public void Connect(object item)
        {
            _model.Connect(item);
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
                    _dispatcher.Invoke(() =>
                    {
                        _peripherals.Clear();
                        _model.Peripherals.ForEach((a) =>
                        {
                            _peripherals.Add(a);
                        });
                    });
                    break;
                case nameof(HostModel.Token):
                    _configModel.DataModel.Token = _model.Token;
                    break;
                case nameof(HostModel.Favorite):
                    _configModel.DataModel.Favorite = _model.Favorite;
                    break;
                default:
                    OnPropertyChanged(e.PropertyName);
                    break;
            }
        }

        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

    }
}
