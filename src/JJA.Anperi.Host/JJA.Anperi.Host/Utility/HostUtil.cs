using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json.Linq;

namespace JJA.Anperi.Host.Utility
{
    public static class HostUtil
    {

        public static bool TryGetCastValue<TResult>(this JObject obj, string key, out TResult value)
        {
            bool result = false;
            if (obj.TryGetValue(key, out JToken dyn))
            {
                try
                {
                    value = dyn.Value<TResult>();
                    result = true;
                }
                catch (RuntimeBinderException)
                {
                    value = default(TResult);
                }
            }
            else
            {
                value = default(TResult);
            }
            return result;
        }

    }
}
