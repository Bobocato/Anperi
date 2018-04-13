using System;
using System.Collections.Generic;
using System.Text;
using JJA.Anperi.Api;

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
        private static JsonApiObject CreateContextServer(JsonApiMessageTypes msgType, HostRequestCode msgCode, object data = null)
        {
            return new JsonApiObject(JsonApiContextTypes.server, msgType, msgCode.ToString(), data);
        }
        
        public static JsonApiObject CreatePairingRequest(string code)
        {
            if (code == null) throw new ArgumentNullException(nameof(code));

            return CreateContextServer(JsonApiMessageTypes.request, HostRequestCode.pair, new { code });
        }
        public static JsonApiObject CreatePairingResponse(bool success)
        {
            return CreateContextServer(JsonApiMessageTypes.response, HostRequestCode.pair, new { success });
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
            return CreateContextServer(JsonApiMessageTypes.response, HostRequestCode.get_available_peripherals, new { devices });
        }

        public static JsonApiObject CreateConnectToPeripheralRequest(int id)
        {
            return CreateContextServer(JsonApiMessageTypes.request, HostRequestCode.connect_to_peripheral, new { id });
        }
        public static JsonApiObject CreateConnectToPeripheralResponse(bool success)
        {
            return CreateContextServer(JsonApiMessageTypes.response, HostRequestCode.connect_to_peripheral, new { success });
        }

        public static JsonApiObject CreateDisconnectFromPeripheralRequest()
        {
            return CreateContextServer(JsonApiMessageTypes.request, HostRequestCode.disconnect_from_peripheral);
        }
        public static JsonApiObject CreateDisconnectFromPeripheralResponse(bool success)
        {
            return CreateContextServer(JsonApiMessageTypes.response, HostRequestCode.disconnect_from_peripheral, new { success });
        }
    }
}