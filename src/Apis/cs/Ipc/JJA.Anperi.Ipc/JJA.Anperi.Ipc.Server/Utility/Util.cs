using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace JJA.Anperi.Ipc.Server.Utility
{
    public static class Util
    {
        public static void TraceException(string line1, Exception ex)
        {
            Trace.TraceError($"{line1}: {ex.GetType()}\nMessage: {ex.Message}\nStack Trace:\n{ex.StackTrace}");
        }
    }
}
