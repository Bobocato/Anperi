using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using AnperiRemote.Annotations;
using AnperiRemote.Utility;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace AnperiRemote.Model
{
    class VolumeModel : INotifyPropertyChanged
    {
        private Guid _guid;
        private int _lastSetVolume = -1;
        public static VolumeModel Instance => _instance.Value;
        private static readonly Lazy<VolumeModel> _instance = new Lazy<VolumeModel>(() =>
        {
            var instance = new VolumeModel();
            instance.Init();
            return instance;
        });

        private MMDeviceEnumerator _deviceEnum;
        private MMDevice _defaultDevice;

        private void Init()
        {
            _guid = Guid.NewGuid();
            _deviceEnum = new MMDeviceEnumerator();
            _deviceEnum.RegisterEndpointNotificationCallback(new MMDeviceNotis(this));
            if (_deviceEnum.HasDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia))
            {
                _defaultDevice = _deviceEnum.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            }
            AnperiModel.Instance.VolumeChanged += (sender, args) => Volume = args.Volume;
        }

        public int Volume
        {
            get => _defaultDevice == null ? 0 : (int) (_defaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar * 100);
            set
            {
                if (value < 0 || value > 100) throw new ArgumentOutOfRangeException(nameof(value));
                if (_lastSetVolume == value) return;
                if (_defaultDevice != null)
                {
                    try
                    {
                        _defaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar = (float)value / 100.0f;
                        _lastSetVolume = value;
                        AnperiModel.Instance.UpdateUiVolume(value).ContinueWith(t =>
                        {
                            Trace.TraceError("Error while setting volume on peripheral UI. {0} - {1}", t?.Exception?.InnerExceptions[0].GetType().ToString(), t?.Exception?.InnerExceptions[0].Message);
                        }, TaskContinuationOptions.OnlyOnFaulted);
                        OnPropertyChanged();
                    }
                    catch (System.Runtime.InteropServices.COMException e)
                    {
                        Trace.TraceError("Error setting volume.");
                    }
                    
                }
            }
        }

        private void AudioEndpointVolume_OnVolumeNotification(AudioVolumeNotificationData data)
        {
            if (!data.Guid.Equals(_guid)) OnPropertyChanged(nameof(Volume));
        }

        private class MMDeviceNotis : IMMNotificationClient
        {
            private readonly VolumeModel _vm; 
            public MMDeviceNotis(VolumeModel vm)
            {
                _vm = vm;
            }

            public void OnDeviceStateChanged(string deviceId, DeviceState newState) { }

            public void OnDeviceAdded(string pwstrDeviceId) { }

            public void OnDeviceRemoved(string deviceId) { }

            public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
            {
                if (flow == DataFlow.Render && role == Role.Multimedia)
                {
                    if (_vm._defaultDevice != null)
                    {
                        _vm._defaultDevice.AudioEndpointVolume.OnVolumeNotification -=
                            _vm.AudioEndpointVolume_OnVolumeNotification;
                    }
                    _vm._defaultDevice = _vm._deviceEnum.GetDevice(defaultDeviceId);
                    if (_vm._defaultDevice == null) return;
                    _vm._defaultDevice.AudioEndpointVolume.NotificationGuid = _vm._guid;
                    _vm._defaultDevice.AudioEndpointVolume.OnVolumeNotification += _vm.AudioEndpointVolume_OnVolumeNotification;
                    _vm.OnPropertyChanged(nameof(VolumeModel.Volume));
                }
            }

            public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key) { }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
