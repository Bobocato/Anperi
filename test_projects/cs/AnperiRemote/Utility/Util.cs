using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AnperiRemote.Utility
{
    static class Util
    {
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
