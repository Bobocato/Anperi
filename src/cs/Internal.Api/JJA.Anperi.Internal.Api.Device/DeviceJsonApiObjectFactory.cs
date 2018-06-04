using System;
using System.Collections.Generic;

namespace JJA.Anperi.Internal.Api.Device
{
    public enum DeviceRequestCode
    {
        // ReSharper disable InconsistentNaming
        get_info, debug, event_fired, error, set_layout, set_element_param, client_went_away
    }

    public static class DeviceJsonApiObjectFactory
    {
        private static JsonApiObject CreateContextDevice(JsonApiMessageTypes msgType, DeviceRequestCode msgCode, Dictionary<string, dynamic> data = null)
        {
            return new JsonApiObject(JsonApiContextTypes.device, msgType, msgCode.ToString(), data);
        }

        public static JsonApiObject CreateDebug(string msg)
        {
            if (msg == null) throw new ArgumentNullException(nameof(msg));
            Dictionary<string, dynamic> data = new Dictionary<string, dynamic>
            {
                {"msg", msg}
            };
            return CreateContextDevice(JsonApiMessageTypes.message, DeviceRequestCode.debug, data);
        }

        public static JsonApiObject CreateError(string msg)
        {
            if (msg == null) throw new ArgumentNullException(nameof(msg));
            Dictionary<string, dynamic> data = new Dictionary<string, dynamic>
            {
                {"msg", msg}
            };
            return CreateContextDevice(JsonApiMessageTypes.message, DeviceRequestCode.error, data);
        }

        public static JsonApiObject CreateSetLayout(Dictionary<string, dynamic> data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            return CreateContextDevice(JsonApiMessageTypes.message, DeviceRequestCode.set_layout, data);
        }

        public static JsonApiObject CreateSetElementParam(Dictionary<string, dynamic> data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            return CreateContextDevice(JsonApiMessageTypes.message, DeviceRequestCode.set_element_param, data);
        }

        public static JsonApiObject CreateDeviceWentAway()
        {
            return CreateContextDevice(JsonApiMessageTypes.message, DeviceRequestCode.client_went_away);
        }

        public static JsonApiObject CreateGetInfo()
        {
            return CreateContextDevice(JsonApiMessageTypes.request, DeviceRequestCode.get_info);
        }

    }
}
