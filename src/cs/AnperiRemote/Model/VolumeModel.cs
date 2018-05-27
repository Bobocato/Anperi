using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AnperiRemote.Annotations;
using AnperiRemote.Utility;
using JJA.Anperi.Utility;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace AnperiRemote.Model
{
    class VolumeModel : INotifyPropertyChanged
    {
        private Guid _guid;
        private MMDeviceNotis _deviceNotis;
        private int _lastSetVolume = 0;
        private readonly object _syncRootLastDevice = new object(); 
        private int _lastVolumeSetThreadId = -1;
        private MMDevice _lastDevice;
        public static VolumeModel Instance => _instance.Value;
        private static readonly Lazy<VolumeModel> _instance = new Lazy<VolumeModel>(() =>
        {
            var instance = new VolumeModel();
            instance.Init();
            return instance;
        });

        private void Init()
        {
            _guid = Guid.NewGuid();
            _deviceNotis = new MMDeviceNotis(_guid);
            _deviceNotis.DefaultDeviceChanged += _deviceNotis_DefaultDeviceChanged;
            _deviceNotis.VolumeChanged += _deviceNotis_VolumeChanged;
            
            AnperiModel.Instance.VolumeChanged += (sender, args) => Volume = args.Volume;
        }

        private void _deviceNotis_VolumeChanged(object sender, AudioVolumeNotificationData e)
        {
            if (!e.Guid.Equals(_guid)) OnPropertyChanged(nameof(Volume));
        }

        private void _deviceNotis_DefaultDeviceChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(Volume));
        }

        private static MMDevice GetDefaultDevice(Guid guid, MMDeviceEnumerator deviceEnumerator = null)
        {
            if (deviceEnumerator == null) deviceEnumerator = new MMDeviceEnumerator();
            try
            {
                MMDevice dev = null;
                if (deviceEnumerator.HasDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia))
                {
                    dev = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                    dev.AudioEndpointVolume.NotificationGuid = guid;
                }
                return dev;
            }
            catch (Exception e)
            {
                Util.TraceException("Error getting default audio device", e);
            }
            return null;
        }

        private MMDevice GetThreadsafeDefaultDevice()
        {
            lock (_syncRootLastDevice)
            {
                if (_lastVolumeSetThreadId != Thread.CurrentThread.ManagedThreadId)
                {
                    _lastVolumeSetThreadId = Thread.CurrentThread.ManagedThreadId;
                    _lastDevice = GetDefaultDevice(_guid);
                }
                return _lastDevice;
            }
        }

        public int Volume
        {
            get
            {
                MMDevice defaultDevice = GetThreadsafeDefaultDevice();
                if (defaultDevice == null) return 0;
                
                int volume = _lastSetVolume;
                try
                {
                    volume = (int) (defaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar * 100);
                }
                catch (Exception ex)
                {
                    Util.TraceException("Exception thrown while retrieving audio volume", ex);
                }
                return volume;
            }
            set
            {
                if (value < 0 || value > 100) throw new ArgumentOutOfRangeException(nameof(value));
                if (_lastSetVolume == value) return;
                MMDevice defaultDevice = GetThreadsafeDefaultDevice();
                if (defaultDevice != null)
                {
                    try
                    {
                        defaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar = (float)value / 100.0f;
                        _lastSetVolume = value;
                        AnperiModel.Instance.UpdateUiVolume(value).ContinueWith(t =>
                        {
                            Trace.TraceError("Error while setting volume on peripheral UI. {0} - {1}", t?.Exception?.InnerExceptions[0].GetType().ToString(), t?.Exception?.InnerExceptions[0].Message);
                        }, TaskContinuationOptions.OnlyOnFaulted);
                        OnPropertyChanged();
                    }
                    catch (System.Runtime.InteropServices.COMException)
                    {
                        Trace.TraceError("Error setting volume.");
                    }
                    
                }
            }
        }

        private class MMDeviceNotis : IMMNotificationClient
        {
            private readonly MMDeviceEnumerator _deviceEnum;
            private readonly Guid _guid;
            private MMDevice _defaultDevice;

            public MMDeviceNotis(Guid guid)
            {
                _guid = guid;
                _deviceEnum = new MMDeviceEnumerator();
                _deviceEnum.RegisterEndpointNotificationCallback(this);
                _defaultDevice = GetDefaultDevice(_guid);
                SetVolumeEventHandler(_defaultDevice);
            }

            private void SetVolumeEventHandler(MMDevice device)
            {
                device.AudioEndpointVolume.NotificationGuid = _guid;
                device.AudioEndpointVolume.OnVolumeNotification += OnVolumeChanged;
            }

            public void OnDeviceStateChanged(string deviceId, DeviceState newState) { }

            public void OnDeviceAdded(string pwstrDeviceId) { }

            public void OnDeviceRemoved(string deviceId) { }

            public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
            {
                if (flow == DataFlow.Render && role == Role.Multimedia)
                {
                    if (_defaultDevice != null)
                    {
                        _defaultDevice.AudioEndpointVolume.OnVolumeNotification -=
                            OnVolumeChanged;
                    }
                    _defaultDevice = _deviceEnum.GetDevice(defaultDeviceId);
                    if (_defaultDevice == null) return;
                    SetVolumeEventHandler(_defaultDevice);
                    OnDefaultDeviceChanged();
                }
            }

            public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key) { }

            public event EventHandler DefaultDeviceChanged;

            public event EventHandler<AudioVolumeNotificationData> VolumeChanged;

            protected virtual void OnVolumeChanged(AudioVolumeNotificationData volume)
            {
                VolumeChanged?.Invoke(this, volume);
            }

            protected virtual void OnDefaultDeviceChanged()
            {
                DefaultDeviceChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
