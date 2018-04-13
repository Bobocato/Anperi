using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Newtonsoft.Json;

namespace JJA.Anperi.Api
{
    // ReSharper disable InconsistentNaming
    public enum JsonApiContextTypes
    {
        server, device
    }
    public enum JsonApiMessageTypes
    {
        request, response, message
    }

    public class JsonApiObject
    {
        public JsonApiObject() { }

        public JsonApiObject(JsonApiContextTypes contextType, JsonApiMessageTypes messageType, string messageCode, object data = null)
        {
            context = contextType;
            message_type = messageType;
            message_code = messageCode;
            this.data = data;
        }

        [JsonProperty]
        public JsonApiContextTypes context { get; internal set; }
        [JsonProperty]
        public JsonApiMessageTypes message_type { get; internal set; }
        [JsonProperty]
        public string message_code { get; internal set; }
        [JsonProperty]
        public object data { get; internal set; }

        public string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static JsonApiObject Deserialize(string text)
        {
            return JsonConvert.DeserializeObject<JsonApiObject>(text);
        }
    }
}
