using System;
using System.Collections.Generic;

namespace JJA.Anperi.Internal.Api.Device
{
    public enum DeviceRequestCode
    {
        // ReSharper disable InconsistentNaming
        debug
    }

    public static class DeviceJsonApiObjectFactory
    {
        private static JsonApiObject CreateContextDevice(JsonApiMessageTypes msgType, DeviceRequestCode msgCode, Dictionary<string, dynamic> data = null)
        {
            
            return new JsonApiObject(JsonApiContextTypes.device, msgType, msgCode.ToString(), data);
        }

        public static JsonApiObject CreateDebugRequest(string msg)
        {
            if (msg == null) throw new ArgumentNullException(nameof(msg));
            Dictionary<string, dynamic> data = new Dictionary<string, dynamic>
            {
                {"msg", msg}
            };
            return CreateContextDevice(JsonApiMessageTypes.message, DeviceRequestCode.debug, data);
        }

    }
}
