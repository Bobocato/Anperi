using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace JJA.Anperi.Utility
{
    public static class Util
    {
        /// <summary>
        /// Writes a formatted exception to the trace error stream.
        /// </summary>
        /// <param name="line1">The first line followed by a :</param>
        /// <param name="ex">The exception to write to the log</param>
        public static void TraceException(string line1, Exception ex)
        {
            Trace.TraceError($"{line1}: {ex.GetType()}\nMessage: {ex.Message}\nStack Trace:\n{ex.StackTrace}");
        }
    }
}
