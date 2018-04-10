using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using JJA.Anperi.Api;

namespace JJA.Anperi.PeripheralApi
{
    public enum PeripheralRequestCode
    {
        // ReSharper disable InconsistentNaming
        register, login, get_pairing_code
    }

    public static class PeripheralJsonApiObjectFactory
    {
        public static JsonApiObject CreateLoginRequest(string token)
        {
            if (token == null) throw new ArgumentNullException(nameof(token));
            
            return new JsonApiObject(ContextTypes.server, MessageTypes.request, PeripheralRequestCode.login.ToString(), new { token });
        }
        public static JsonApiObject CreateLoginResponse(bool success)
        {
            return new JsonApiObject(ContextTypes.server, MessageTypes.response, PeripheralRequestCode.login.ToString(), new { success });
        }

        public static JsonApiObject CreateRegisterRequest()
        {
            return new JsonApiObject(ContextTypes.server, MessageTypes.request, PeripheralRequestCode.register.ToString());
        }
        public static JsonApiObject CreateRegisterResponse(string token)
        {
            if (token == null) throw new ArgumentNullException(nameof(token));
            
            return new JsonApiObject(ContextTypes.server, MessageTypes.response, PeripheralRequestCode.register.ToString(), new { token });
        }


        public static JsonApiObject CreateGetPairingCodeRequest()
        {
            return new JsonApiObject(ContextTypes.server, MessageTypes.request, PeripheralRequestCode.register.ToString());
        }

        public static JsonApiObject CreateGetPairingCodeResponse(string code)
        {
            if (code == null) throw new ArgumentNullException(nameof(code));
            
            return new JsonApiObject(ContextTypes.server, MessageTypes.response, PeripheralRequestCode.register.ToString(), new { code });
        }

    }
}
