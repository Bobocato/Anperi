using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
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

        public static string AssemblyDirectory
        {
            get { return Path.GetDirectoryName(FullAssemblyPath); }
        }

        /// <summary>
        /// Needed because the Assembly.Location doesn't handle all special characters (like '#' for example).
        /// </summary>
        public static string FullAssemblyPath
        {
            get
            {
                string codeBasePseudoUrl = Assembly.GetEntryAssembly().CodeBase;
                const string filePrefix3 = @"file:///";
                if (codeBasePseudoUrl.StartsWith(filePrefix3))
                {
                    string sPath = codeBasePseudoUrl.Substring(filePrefix3.Length);
                    return sPath.Replace('/', '\\');
                }
                return Assembly.GetEntryAssembly().Location;
            }
        }

        public static string AssemblyName
        {
            get { return Path.GetFileName(FullAssemblyPath); }
        }

        public static string AssemblyNameWithoutExtension
        {
            get { return Path.GetFileNameWithoutExtension(FullAssemblyPath); }
        }
    }
}
