using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Newtonsoft.Json;

namespace JJA.Anperi.Api
{
    // ReSharper disable InconsistentNaming
    public enum ContextTypes
    {
        server, device
    }
    public enum MessageTypes
    {
        request, response, message
    }

    public class JsonApiObject
    {
        public JsonApiObject() { }

        public JsonApiObject(ContextTypes contextType, MessageTypes messageType, string messageCode, object data = null)
        {
            context = contextType.ToString();
            message_type = messageType.ToString();
            message_code = messageCode;
            this.data = data;
        }

        [JsonProperty]
        public string context { get; internal set; }

        [JsonProperty]
        public string message_type { get; internal set; }

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
