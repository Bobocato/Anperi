using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;

namespace JJA.Anperi.Api.SharedMessages
{
    public enum MessageCode
    {
        // ReSharper disable InconsistentNaming
        error, partner_disconnected
    }

    public static class SharedMessageJsonApiObjectFactory
    {
        public static JsonApiObject CreateError(string message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            
            return new JsonApiObject(ContextTypes.server, MessageTypes.message, MessageCode.error.ToString(), new { message });
        }

        public static JsonApiObject CreatePartnerDisconnected()
        {
            return new JsonApiObject(ContextTypes.server, MessageTypes.message, MessageCode.partner_disconnected.ToString());
        }
    }
}
