using System;
using System.Collections.Generic;

namespace JJA.Anperi.Internal.Api.Peripheral
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
            Dictionary<string, dynamic> data = new Dictionary<string, dynamic>
            {
                {"code", code}
            };
            return new JsonApiObject(JsonApiContextTypes.server, JsonApiMessageTypes.response, PeripheralRequestCode.get_pairing_code.ToString(), data);
        }

    }
}
