using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
        private HostModel _model;
        private ObservableCollection<HostJsonApiObjectFactory.ApiPeripheral> _peripherals;

        public HostViewModel(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            _model = new HostModel();
            _model.PropertyChanged += OnModelPropertyChanged;
            _peripherals = new ObservableCollection<HostJsonApiObjectFactory.ApiPeripheral>();
        }
        
        public bool ButConnect
        {
            get { return _model.ButConnect; }
            set
            {
                _model.ButConnect = value;
                OnPropertyChanged(nameof(ButConnect));
            }
        }

        public bool ButDisconnect
        {
            get { return _model.ButDisconnect; }
            set
            {
                _model.ButDisconnect = value;
                OnPropertyChanged(nameof(ButDisconnect));
            }
        }

        public string Info1
        {
            get { return _model.Info1;}
            set
            {
                _model.Info1 = value;
                OnPropertyChanged(nameof(Info1));
            }
        }

        public string Info2
        {
            get { return _model.Info2; }
            set
            {
                _model.Info2 = value;
                OnPropertyChanged(nameof(Info2));
            }
        }

        public string Info3
        {
            get{ return _model.Info3; }
            set
            {
                _model.Info3 = value;
                OnPropertyChanged(nameof(Info3));
            }
        }

        public string ConnectedTo
        {
            get { return _model.ConnectedTo; }
            set
            {
                _model.ConnectedTo = value;
                OnPropertyChanged(nameof(ConnectedTo));
            }
        }

        public ObservableCollection<HostJsonApiObjectFactory.ApiPeripheral> Peripherals => _peripherals;

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
            if (e.PropertyName == nameof(HostModel.Peripherals))
            {
                _dispatcher.Invoke(() =>
                {
                    _peripherals.Clear();
                    _model.Peripherals.ForEach((a) =>
                    {
                        _peripherals.Add(a);
                    });
                });
            }
            else
            {
                OnPropertyChanged(e.PropertyName);
            }
        }

        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

    }
}
