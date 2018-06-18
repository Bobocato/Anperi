using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace JJA.Anperi.Internal.Api.Shared
{
    public enum SharedJsonMessageCode
    {
        // ReSharper disable InconsistentNaming
        error, partner_disconnected, partner_connected
    }

    public enum SharedJsonRequestCode
    {
        login, register, set_own_name
    }

    public enum SharedJsonDeviceType
    {
        host, peripheral
    }

    public class JsonLoginData
    {
        [JsonProperty]
        public string token { get; set; }
        [JsonProperty]
        public string device_type { get; set; }
    }

    public static class SharedJsonApiObjectFactory
    {
        public static JsonApiObject CreateError(string message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            Dictionary<string, dynamic> data = new Dictionary<string, dynamic>
            {
                {"message", message}
            };
            return new JsonApiObject(JsonApiContextTypes.server, JsonApiMessageTypes.message, SharedJsonMessageCode.error.ToString(), data);
        }

        public static JsonApiObject CreatePartnerDisconnected()
        {
            return new JsonApiObject(JsonApiContextTypes.server, JsonApiMessageTypes.message, SharedJsonMessageCode.partner_disconnected.ToString());
        }

        public static JsonApiObject CreatePartnerConnected(string partnerName)
        {
            return new JsonApiObject(JsonApiContextTypes.server, JsonApiMessageTypes.message, SharedJsonMessageCode.partner_connected.ToString(), new Dictionary<string, dynamic> { { "name", partnerName } });
        }

        public static JsonApiObject CreateLoginRequest(string token, SharedJsonDeviceType type)
        {
            if (token == null) throw new ArgumentNullException(nameof(token));
            Dictionary<string, dynamic> data = new Dictionary<string, dynamic>
            {
                { "device_type", type.ToString() },
                { "token", token }
            };
            return new JsonApiObject(JsonApiContextTypes.server, JsonApiMessageTypes.request, SharedJsonRequestCode.login.ToString(), data);
        }
        public static JsonApiObject CreateLoginResponse(bool success, string name)
        {
            Dictionary<string, dynamic> data = new Dictionary<string, dynamic>
            {
                {"success", success},
                {"name", name}
            };
            return new JsonApiObject(JsonApiContextTypes.server, JsonApiMessageTypes.response, SharedJsonRequestCode.login.ToString(), data);
        }

        public static JsonApiObject CreateRegisterRequest(SharedJsonDeviceType type, string name)
        {
            Dictionary<string, dynamic> data = new Dictionary<string, dynamic>
            {
                {"device_type", type.ToString()},
                {"name", name}
            };
            return new JsonApiObject(JsonApiContextTypes.server, JsonApiMessageTypes.request, SharedJsonRequestCode.register.ToString(), data);
        }
        public static JsonApiObject CreateRegisterResponse(string token, string name)
        {
            if (token == null) throw new ArgumentNullException(nameof(token));
            Dictionary<string, dynamic> data = new Dictionary<string, dynamic>
            {
                {"token", token},
                {"name", name}
            };
            return new JsonApiObject(JsonApiContextTypes.server, JsonApiMessageTypes.response, SharedJsonRequestCode.register.ToString(), data);
        }

        public static JsonApiObject CreateChangeOwnNameRequest(string name)
        {
            Dictionary<string, dynamic> data = new Dictionary<string, dynamic>
            {
                {"name", name}
            };
            return new JsonApiObject(JsonApiContextTypes.server, JsonApiMessageTypes.request, SharedJsonRequestCode.set_own_name.ToString(), data);
        }
        public static JsonApiObject CreateChangeOwnNameResponse(bool success, string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            Dictionary<string, dynamic> data = new Dictionary<string, dynamic>
            {
                {"success", success},
                {"name", name}
            };
            return new JsonApiObject(JsonApiContextTypes.server, JsonApiMessageTypes.response, SharedJsonRequestCode.set_own_name.ToString(), data);
        }
    }
}
