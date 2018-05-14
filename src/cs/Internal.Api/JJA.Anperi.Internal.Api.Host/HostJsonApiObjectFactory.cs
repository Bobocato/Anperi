using System;
using System.Collections.Generic;

namespace JJA.Anperi.Internal.Api.Host
{
    // ReSharper disable InconsistentNaming
    public enum HostRequestCode
    {
        pair, unpair, get_available_peripherals,
        connect_to_peripheral, disconnect_from_peripheral,
        change_peripheral_name
    }

    public enum HostMessage
    {
        paired_peripheral_logged_on, paired_peripheral_logged_off
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
            public int id { get; set; }
            public string name { get; set; }
            public bool online { get; set; }
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
        public static JsonApiObject CreateConnectToPeripheralResponse(bool success, int periId)
        {
            Dictionary<string, dynamic> data = new Dictionary<string, dynamic>
            {
                {"success", success},
                {"id", periId}
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

        public static JsonApiObject CreateUnpairFromPeripheralRequest(int id)
        {
            Dictionary<string, dynamic> data = new Dictionary<string, dynamic>
            {
                {"id", id}
            };
            return CreateContextServer(JsonApiMessageTypes.request, HostRequestCode.unpair, data);
        }
        public static JsonApiObject CreateUnpairFromPeripheralResponse(bool success)
        {
            Dictionary<string, dynamic> data = new Dictionary<string, dynamic>
            {
                {"success", success}
            };
            return CreateContextServer(JsonApiMessageTypes.response, HostRequestCode.unpair, data);
        }

        public static JsonApiObject CreatePairedPeripheralLoggedOnMessage(int id)
        {
            Dictionary<string, dynamic> data = new Dictionary<string, dynamic>
            {
                {"id", id}
            };
            return new JsonApiObject(JsonApiContextTypes.server, JsonApiMessageTypes.message, HostMessage.paired_peripheral_logged_on.ToString(), data);
        }
        public static JsonApiObject CreatePairedPeripheralLoggedOffMessage(int id)
        {
            Dictionary<string, dynamic> data = new Dictionary<string, dynamic>
            {
                {"id", id}
            };
            return new JsonApiObject(JsonApiContextTypes.server, JsonApiMessageTypes.message, HostMessage.paired_peripheral_logged_off.ToString(), data);
        }

        public static JsonApiObject CreateChangeNameRequest(int id, string name)
        {
            Dictionary<string, dynamic> data = new Dictionary<string, dynamic>
            {
                { "id", id },
                {"name", name}
            };
            return new JsonApiObject(JsonApiContextTypes.server, JsonApiMessageTypes.request, HostRequestCode.change_peripheral_name.ToString(), data);
        }
        public static JsonApiObject CreateChangeNameResponse(bool success, string name, int id)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            Dictionary<string, dynamic> data = new Dictionary<string, dynamic>
            {
                {"success", success},
                {"name", name},
                { "id", id }
            };
            return new JsonApiObject(JsonApiContextTypes.server, JsonApiMessageTypes.response, HostRequestCode.change_peripheral_name.ToString(), data);
        }
    }
}