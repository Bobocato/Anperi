using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using AnperiRemote.Model;
using AnperiRemote.Utility;
using Newtonsoft.Json;
using Util = JJA.Anperi.Utility.Util;

namespace AnperiRemote.DAL
{
    sealed class SettingsFile
    {
        private static SettingsFile _instance;

        public static SettingsFile Instance => _instance ??
                                                (_instance = new SettingsFile(Path.Combine(Util.AssemblyDirectory,
                                                    "settings.json")));

        private readonly string _path;

        public SettingsModel SettingsModel { get; private set; }

        private SettingsFile(string path)
        {
            Trace.TraceInformation($"Initializing SettingsFile with {path}.");
            _path = path;
            SettingsModel = Load();
        }

        private SettingsModel Load()
        {
            bool fileExists = File.Exists(_path);
            if (!fileExists)
            {
                SettingsModel = new SettingsModel();
            }
            else
            {
                try
                {
                    string settingsText = File.ReadAllText(_path);
                    SettingsModel = JsonConvert.DeserializeObject<SettingsModel>(settingsText);
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error reading settings file:\n{0}", $"\t{ex.GetType().FullName}: {ex.Message}");
                    try
                    {
                        File.Delete(_path);
                    } catch(Exception ex2)
                    {
                        Trace.TraceError("Error reading settings file:\n{0}", $"\t{ex2.GetType().FullName}: {ex2.Message}");
                    }
                }
            }
            return this.SettingsModel;
        }

        public void Save()
        {
            string settingsText = JsonConvert.SerializeObject(SettingsModel, Formatting.Indented);
            try
            {
                File.WriteAllText(_path, settingsText);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error reading settings file:\n{0}", $"\t{ex.GetType().FullName}: {ex.Message}");
            }
        }
    }
}
