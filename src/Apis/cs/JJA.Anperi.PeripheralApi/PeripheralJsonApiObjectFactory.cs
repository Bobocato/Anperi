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
        get_pairing_code
    }

    public static class PeripheralJsonApiObjectFactory
    {
        public static JsonApiObject CreateGetPairingCodeRequest()
        {
            return new JsonApiObject(JsonApiContextTypes.server, JsonApiMessageTypes.request, PeripheralRequestCode.get_pairing_code.ToString());
        }

        public static JsonApiObject CreateGetPairingCodeResponse(string code)
        {
            if (code == null) throw new ArgumentNullException(nameof(code));
            
            return new JsonApiObject(JsonApiContextTypes.server, JsonApiMessageTypes.response, PeripheralRequestCode.get_pairing_code.ToString(), new { code });
        }

    }
}
