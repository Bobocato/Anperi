using System;
using System.Collections.Generic;
using System.Text;
using JJA.Anperi.Api;

namespace JJA.Anperi.DeviceApi
{
    public enum DeviceRequestCode
    {
        // ReSharper disable InconsistentNaming
        debug
    }

    public static class DeviceJsonApiObjectFactory
    {
        private static JsonApiObject CreateContextDevice(MessageTypes msgType, DeviceRequestCode msgCode, object data = null)
        {
            return new JsonApiObject(ContextTypes.device, msgType, msgCode.ToString(), data);
        }

        public static JsonApiObject CreateDebugRequest(string msg)
        {
            if (msg == null) throw new ArgumentNullException(nameof(msg));

            return CreateContextDevice(MessageTypes.message, DeviceRequestCode.debug, new { msg });
        }

    }
}
