using System;
using System.Collections.Generic;
using System.Text;
using JJA.Anperi.Api;
using Newtonsoft.Json;

namespace JJA.Anperi.HostApi
{
    public enum HostRequestCode
    {
        // ReSharper disable InconsistentNaming
        pair, unpair, get_available_peripherals,
        connect_to_peripheral, disconnect_from_peripheral
    }

    public static class HostJsonApiObjectFactory
    {
        private static JsonApiObject CreateContextServer(JsonApiMessageTypes msgType, HostRequestCode msgCode, Dictionary<string, dynamic> data = null)
        {
            return new JsonApiObject(JsonApiContextTypes.server, msgType, msgCode.ToString(), data);
        }
        
        public static JsonApiObject CreatePairingRequest(string code)
        {
            if (code == null) throw new ArgumentNullException(nameof(code));
            Dictionary<string, dynamic> data = new Dictionary<string, dynamic>
            {
                {"code", code}
            };
            return CreateContextServer(JsonApiMessageTypes.request, HostRequestCode.pair, data);
        }
        public static JsonApiObject CreatePairingResponse(bool success)
        {
            Dictionary<string, dynamic> data = new Dictionary<string, dynamic>
            {
                {"success", success}
            };
            return CreateContextServer(JsonApiMessageTypes.response, HostRequestCode.pair, data);
        }

        public class ApiPeripheral
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
        public static JsonApiObject CreateAvailablePeripheralRequest()
        {
            return CreateContextServer(JsonApiMessageTypes.request, HostRequestCode.get_available_peripherals);
        }
        public static JsonApiObject CreateAvailablePeripheralResponse(IEnumerable<ApiPeripheral> devices)
        {
            Dictionary<string, dynamic> data = new Dictionary<string, dynamic>
            {
                {"devices", devices}
            };
            return CreateContextServer(JsonApiMessageTypes.response, HostRequestCode.get_available_peripherals, data);
        }

        public static JsonApiObject CreateConnectToPeripheralRequest(int id)
        {
            Dictionary<string, dynamic> data = new Dictionary<string, dynamic>
            {
                {"id", id}
            };
            return CreateContextServer(JsonApiMessageTypes.request, HostRequestCode.connect_to_peripheral, data);
        }
        public static JsonApiObject CreateConnectToPeripheralResponse(bool success)
        {
            Dictionary<string, dynamic> data = new Dictionary<string, dynamic>
            {
                {"success", success}
            };
            return CreateContextServer(JsonApiMessageTypes.response, HostRequestCode.connect_to_peripheral, data);
        }

        public static JsonApiObject CreateDisconnectFromPeripheralRequest()
        {
            return CreateContextServer(JsonApiMessageTypes.request, HostRequestCode.disconnect_from_peripheral);
        }
        public static JsonApiObject CreateDisconnectFromPeripheralResponse(bool success)
        {
            Dictionary<string, dynamic> data = new Dictionary<string, dynamic>
            {
                {"success", success}
            };
            return CreateContextServer(JsonApiMessageTypes.response, HostRequestCode.disconnect_from_peripheral, data);
        }
    }
}