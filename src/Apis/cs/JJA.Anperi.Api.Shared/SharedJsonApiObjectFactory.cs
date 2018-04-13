using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;

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

    public static class SharedJsonApiObjectFactory
    {
        public static JsonApiObject CreateError(string message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            
            return new JsonApiObject(JsonApiContextTypes.server, JsonApiMessageTypes.message, SharedJsonMessageCode.error.ToString(), new { message });
        }

        public static JsonApiObject CreatePartnerDisconnected()
        {
            return new JsonApiObject(JsonApiContextTypes.server, JsonApiMessageTypes.message, SharedJsonMessageCode.partner_disconnected.ToString());
        }

        public static JsonApiObject CreateLoginRequest(string token)
        {
            if (token == null) throw new ArgumentNullException(nameof(token));

            return new JsonApiObject(JsonApiContextTypes.server, JsonApiMessageTypes.request, SharedJsonRequestCode.login.ToString(), new { token });
        }
        public static JsonApiObject CreateLoginResponse(bool success)
        {
            return new JsonApiObject(JsonApiContextTypes.server, JsonApiMessageTypes.response, SharedJsonRequestCode.login.ToString(), new { success });
        }

        public static JsonApiObject CreateRegisterRequest()
        {
            return new JsonApiObject(JsonApiContextTypes.server, JsonApiMessageTypes.request, SharedJsonRequestCode.register.ToString());
        }
        public static JsonApiObject CreateRegisterResponse(string token)
        {
            if (token == null) throw new ArgumentNullException(nameof(token));

            return new JsonApiObject(JsonApiContextTypes.server, JsonApiMessageTypes.response, SharedJsonRequestCode.register.ToString(), new { token });
        }
    }
}
