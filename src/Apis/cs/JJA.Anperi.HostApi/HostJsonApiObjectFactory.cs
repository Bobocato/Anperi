using System;
using System.Collections.Generic;
using System.Text;
using JJA.Anperi.Api;

namespace JJA.Anperi.HostApi
{
    public enum HostRequestCode
    {
        // ReSharper disable InconsistentNaming
        register, login,
        pair, unpair, get_available_peripherals,
        connect_to_peripheral, disconnect_from_peripheral
    }

    public static class HostJsonApiObjectFactory
    {
        private static JsonApiObject CreateContextServer(MessageTypes msgType, HostRequestCode msgCode, object data = null)
        {
            return new JsonApiObject(ContextTypes.server, msgType, msgCode.ToString(), data);
        }

        public static JsonApiObject CreateLoginRequest(string token)
        {
            if (token == null) throw new ArgumentNullException(nameof(token));

            return CreateContextServer(MessageTypes.request, HostRequestCode.login, new {token});
        }
        public static JsonApiObject CreateLoginResponse(bool success)
        {
            return CreateContextServer(MessageTypes.response, HostRequestCode.login, new { success });
        }

        public static JsonApiObject CreatePairingRequest(string code)
        {
            if (code == null) throw new ArgumentNullException(nameof(code));

            return CreateContextServer(MessageTypes.request, HostRequestCode.pair, new { code });
        }
        public static JsonApiObject CreatePairingResponse(bool success)
        {
            return CreateContextServer(MessageTypes.response, HostRequestCode.pair, new { success });
        }

        public class Peripheral
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
        public static JsonApiObject CreateAvailablePeripheralRequest()
        {
            return CreateContextServer(MessageTypes.request, HostRequestCode.get_available_peripherals);
        }
        public static JsonApiObject CreateAvailablePeripheralResponse(IEnumerable<Peripheral> devices)
        {
            return CreateContextServer(MessageTypes.response, HostRequestCode.get_available_peripherals, new { devices });
        }

        public static JsonApiObject CreateConnectToPeripheralRequest(int id)
        {
            return CreateContextServer(MessageTypes.request, HostRequestCode.connect_to_peripheral, new { id });
        }
        public static JsonApiObject CreateConnectToPeripheralResponse(bool success)
        {
            return CreateContextServer(MessageTypes.response, HostRequestCode.connect_to_peripheral, new { success });
        }

        public static JsonApiObject CreateDisconnectFromPeripheralRequest()
        {
            return CreateContextServer(MessageTypes.request, HostRequestCode.disconnect_from_peripheral);
        }
        public static JsonApiObject CreateDisconnectFromPeripheralResponse(bool success)
        {
            return CreateContextServer(MessageTypes.response, HostRequestCode.disconnect_from_peripheral, new { success });
        }
    }
}