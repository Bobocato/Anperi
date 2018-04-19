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

        /// <summary>
        /// TryGetValue with typecheck for dynamic dictionary.
        /// </summary>
        /// <returns>if the key existed and the dynamic was of the given type</returns>
        internal static bool TryGetValue<TK, T>(this Dictionary<TK, dynamic> dict, TK key, out T val)
        {
            bool result = false;
            if (dict.TryGetValue(key, out dynamic dyn))
            {
                try
                {
                    val = (T)dyn;
                    result = true;
                }
                catch (RuntimeBinderException)
                {
                    val = default(T);
                }
            }
            else
            {
                val = default(T);
            }
            return result;
        }

    }
}
