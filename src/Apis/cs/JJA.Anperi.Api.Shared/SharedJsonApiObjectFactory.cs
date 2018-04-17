using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using Newtonsoft.Json;

namespace JJA.Anperi.Api.Shared
{
    public enum SharedJsonMessageCode
    {
        // ReSharper disable InconsistentNaming
        error, partner_disconnected
    }

    public enum SharedJsonRequestCode
    {
        login, register
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

        public static JsonApiObject CreateLoginRequest(string token, SharedJsonDeviceType type)
        {
            if (token == null) throw new ArgumentNullException(nameof(token));
            Dictionary<string, dynamic> data = new Dictionary<string, dynamic>
            {
                { "device_type", type },
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
    }
}
