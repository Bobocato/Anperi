using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace NAudioTests
{
    class Program
    {
        static MMDeviceEnumerator _enumerator = new MMDeviceEnumerator();
        private static MMDevice _defaultDevice;
        static void Main(string[] args)
        {
            Console.WriteLine("List of all devices:");
            
            foreach (var wasapi in _enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.All))
            {
                Console.WriteLine($"{wasapi.DataFlow} {wasapi.FriendlyName} {wasapi.DeviceFriendlyName} {wasapi.State} {wasapi.ID}");
            }

            Console.WriteLine("\nAll plugged in and active playback devices:");
            foreach (var wasapi in _enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
            {
                Console.WriteLine($"{wasapi.DataFlow} {wasapi.FriendlyName} {wasapi.DeviceFriendlyName} {wasapi.State} {wasapi.ID}");
            }


            Console.WriteLine("\nDefault playback device:");
            var device = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
            Console.WriteLine($"Console: {device.DataFlow} {device.FriendlyName} {device.DeviceFriendlyName} {device.State} {device.ID}");
            device = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Communications);
            Console.WriteLine($"Communications: {device.DataFlow} {device.FriendlyName} {device.DeviceFriendlyName} {device.State} {device.ID}");
            device = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            _defaultDevice = device;
            _defaultDevice.AudioEndpointVolume.OnVolumeNotification += AudioEndpointVolume_OnVolumeNotification;
            Console.WriteLine($"Multimedia: {device.DataFlow} {device.FriendlyName} {device.DeviceFriendlyName} {device.State} {device.ID}");

            _enumerator.RegisterEndpointNotificationCallback(new NAudioEnumNotifs());

            Console.ReadLine();
        }

        private static void AudioEndpointVolume_OnVolumeNotification(AudioVolumeNotificationData data)
        {
            Console.WriteLine("normal: " + _defaultDevice.AudioEndpointVolume.MasterVolumeLevel);
            Console.WriteLine("scalar: " + _defaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar);
            Console.WriteLine("Default device changed volume to: " + data.MasterVolume);
        }

        class NAudioEnumNotifs : IMMNotificationClient
        {
            public void OnDeviceStateChanged(string deviceId, DeviceState newState)
            {
                Console.WriteLine($"OnDeviceStateChanged: {deviceId} {newState.ToString()}");
            }

            public void OnDeviceAdded(string pwstrDeviceId)
            {
                Console.WriteLine($"Device got added: {pwstrDeviceId}");
            }

            public void OnDeviceRemoved(string deviceId)
            {
                Console.WriteLine($"Device got removed: {deviceId}");
            }

            public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
            {
                if (flow == DataFlow.Render && role == Role.Multimedia)
                {
                    _defaultDevice.AudioEndpointVolume.OnVolumeNotification -= AudioEndpointVolume_OnVolumeNotification;
                    _defaultDevice = _enumerator.GetDevice(defaultDeviceId);
                    _defaultDevice.AudioEndpointVolume.OnVolumeNotification += AudioEndpointVolume_OnVolumeNotification;
                }
                Console.WriteLine($"Default device changed to: {defaultDeviceId} {flow.ToString()} {role.ToString()}");
            }

            public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
            {
                //Console.WriteLine($"Device with ID: {pwstrDeviceId} changed the property: {key.formatId}:{key.propertyId} to {_enumerator.GetDevice(pwstrDeviceId).Properties[key].Value.ToString()}");
            }
        }
    }
}
