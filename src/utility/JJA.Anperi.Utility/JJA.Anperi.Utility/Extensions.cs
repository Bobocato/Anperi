using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.CSharp.RuntimeBinder;

namespace JJA.Anperi.Utility
{
    public static class Extensions
    {
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
