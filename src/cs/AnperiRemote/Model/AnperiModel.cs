using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using AnperiRemote.Annotations;
using AnperiRemote.Utility;
using JJA.Anperi.Lib;
using JJA.Anperi.Lib.Elements;
using JJA.Anperi.Lib.Message;
using JJA.Anperi.Utility;

namespace AnperiRemote.Model
{
    class AnperiModel : INotifyPropertyChanged
    {
        //this should not be lazy-loaded cause it's supposed to run always
        public static readonly AnperiModel Instance = new AnperiModel();
        private readonly Anperi _anperi;
        private readonly object _syncRootLayout = new object();
        private RootGrid _currentLayout;
        private readonly SettingsModel _settings;
        private bool _shuttingDown = false;

        public static void Init() { }

        private enum Elements : int
        {
            ButtonShutdown = 0,
            SliderVolume = 1
        }
        private readonly Dictionary<Elements, string> _elementIds = new Dictionary<Elements, string>
        {
            { Elements.ButtonShutdown, "btnShutdown" },
            { Elements.SliderVolume, "sliderVolume" }
        };

        private AnperiModel()
        {
            _anperi = new Anperi();
            _anperi.Connected += _anperi_Connected;
            _anperi.Disconnected += _anperi_Disconnected;
            _anperi.Message += _anperi_Message;
            _anperi.ControlLost += _anperi_ControlLost;
            _anperi.HostNotClaimed += _anperi_HostNotClaimed;
            _anperi.PeripheralConnected += _anperi_PeripheralConnected;
            _anperi.PeripheralDisconnected += _anperi_PeripheralDisconnected;
            _settings = SettingsModel.Instance;
            _settings.PropertyChanged += _settings_PropertyChanged;
        }

        private void _settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            SetLayout((SettingsModel) sender).ContinueWith(t => { Util.TraceException("Error setting layout", t?.Exception?.InnerException); }, TaskContinuationOptions.OnlyOnFaulted);
        }

        public event EventHandler<AnperiVolumeEventArgs> VolumeChanged;

        public async Task SetLayout(SettingsModel sm)
        {
            lock (_syncRootLayout)
            {
                _currentLayout = new RootGrid();
                _currentLayout.elements = new List<Element>();
                int currRow = 0;
                if (sm.ShutdownControlEnabled) _currentLayout.elements.Add(new Button { id = _elementIds[Elements.ButtonShutdown], column = 0, row = currRow++, text = "Shutdown" });
                if (sm.VolumeControlEnabled)
                {
                    Grid row = new Grid { column = 0, row = currRow++ };
                    row.elements.Add(new Label { id = "labelVolume", column = 0, row = 0, text = "Volume:" });
                    row.elements.Add(new Slider { id = _elementIds[Elements.SliderVolume], column = 1, row = 1, min = 0, max = 100, step_size = 2, progress = VolumeModel.Instance.Volume});
                    _currentLayout.elements.Add(row);
                }
            }
            if (_anperi.HasControl && _anperi.IsPeripheralConnected)
            {
                RootGrid grid;
                lock (_syncRootLayout)
                {
                    grid = _currentLayout;
                }
                await _anperi.SetLayout(grid, ScreenOrientation.portrait).ConfigureAwait(false);
            }
        }

        public async Task UpdateUiVolume(int volume)
        {
            if (_anperi.HasControl && _anperi.IsPeripheralConnected)
            {
                await _anperi.UpdateElementParam(_elementIds[Elements.SliderVolume], "progress", volume);
            }
        }

        private void _anperi_PeripheralDisconnected(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(IsPeripheralConnected));
        }

        private async void _anperi_PeripheralConnected(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(IsPeripheralConnected));
            await SetLayout(SettingsModel.Instance).ConfigureAwait(false);
        }

        private async void _anperi_HostNotClaimed(object sender, EventArgs e)
        {
            await _anperi.ClaimControl().ConfigureAwait(false);
            await SetLayout(_settings);
            OnPropertyChanged(nameof(HasControl));
        }

        private void _anperi_ControlLost(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(HasControl));
        }

        private void _anperi_Message(object sender, AnperiMessageEventArgs e)
        {
            switch (e.Message)
            {
                case EventFiredAnperiMessage eventMessage:
                    switch (eventMessage.Event)
                    {
                        case EventFiredAnperiMessage.EventType.on_click:
                            if (_shuttingDown)
                            {
                                ShutdownHelper.AbortShutdown();
                                _shuttingDown = false;
                            }
                            else
                            {
                                ShutdownHelper.Shutdown(10);
                                _shuttingDown = true;
                            }
                            _anperi.UpdateElementParam(_elementIds[Elements.ButtonShutdown], "text", _shuttingDown ? "Abort Shutdown" : "Shutdown");
                            break;
                        case EventFiredAnperiMessage.EventType.on_click_long:
                            break;
                        case EventFiredAnperiMessage.EventType.on_change:
                            if (eventMessage.ElementId == _elementIds[Elements.SliderVolume])
                            {
                                if (eventMessage.EventData.TryGetValue(nameof(Slider.progress), out int prog))
                                {
                                    OnVolumeChanged(eventMessage.ElementId, prog);
                                }
                                else
                                {
                                    Trace.TraceInformation("Error parsing volume from message.");
                                }
                            }
                            break;
                        case EventFiredAnperiMessage.EventType.on_input:
                            break;
                        default:
                            Trace.TraceWarning($"Got not handled event: {eventMessage.Event.ToString()} from: {eventMessage.ElementId}");
                            break;
                    }
                    break;
            }
        }

        private void _anperi_Disconnected(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(IsConnected));
            OnPropertyChanged(nameof(IsPeripheralConnected));
            OnPropertyChanged(nameof(HasControl));
        }

        private void _anperi_Connected(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(IsConnected));
            OnPropertyChanged(nameof(IsPeripheralConnected));
            OnPropertyChanged(nameof(HasControl));
            SetLayout(_settings).ContinueWith(t => { Util.TraceException("Error setting layout", t?.Exception?.InnerException); }, TaskContinuationOptions.OnlyOnFaulted);
        }

        public bool IsConnected => _anperi?.IsConnected ?? false;
        public bool IsPeripheralConnected => _anperi?.IsPeripheralConnected ?? false;
        public bool HasControl => _anperi?.HasControl ?? false;

        protected virtual void OnVolumeChanged(string id, int volume)
        {
            VolumeChanged?.Invoke(this, new AnperiVolumeEventArgs(id, volume));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
