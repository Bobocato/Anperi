using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace JJA.Anperi.Internal.Api
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

        public JsonApiObject(JsonApiContextTypes contextType, JsonApiMessageTypes messageType, string messageCode, Dictionary<string, dynamic> data = null)
        {
            context = contextType;
            message_type = messageType;
            message_code = messageCode;
            this.data = data;
        }

        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public JsonApiContextTypes context { get; internal set; }
        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public JsonApiMessageTypes message_type { get; internal set; }
        [JsonProperty]
        public string message_code { get; internal set; }
        [JsonProperty]
        public Dictionary<string, dynamic> data { get; internal set; }

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
