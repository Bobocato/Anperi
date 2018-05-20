using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using JJA.Anperi.Host.Model;
using JJA.Anperi.Utility;
using Microsoft.Win32;

namespace JJA.Anperi.Host.Utility
{
    static class ConfigHandler
    {
        private const string ConfigPath = "settings.data";
        private const string SaltPath = "salt.data";
        private static byte[] _salt;

        public static HostDataModel Load()
        {
            bool fileExists = File.Exists(ConfigPath);
            if (!fileExists) return new HostDataModel();

            HostDataModel model = null;

            if (File.Exists(SaltPath))
            {
                _salt = File.ReadAllBytes(SaltPath);
                try
                {
                    byte[] file = File.ReadAllBytes(ConfigPath);
                    file = Unprotect(file);
                    using (var ms = new MemoryStream(file))
                    {
                        var bf = new BinaryFormatter();
                        model = (HostDataModel)bf.Deserialize(ms);
                    }
                }
                catch (Exception e)
                {
                    Util.TraceException("Error loading HostData, restoring defaults ...", e);
                }
            }
            return model ?? new HostDataModel();
        }

        private static byte[] Protect(byte[] data)
        {
            //TODO: Salt = random Byte[], saved in config
            return ProtectedData.Protect(data, _salt, DataProtectionScope.LocalMachine);
        }

        private static byte[] Unprotect(byte[] data)
        {
            //TODO: look protect
            return ProtectedData.Unprotect(data, _salt, DataProtectionScope.LocalMachine);
        }

        public static void Save(HostDataModel dataModel)
        {
            try
            {
                byte[] data;
                CreateNewSalt();
                using (var ms = new MemoryStream())
                {
                    var bf = new BinaryFormatter();
                    bf.Serialize(ms, dataModel);
                    data = ms.ToArray();
                }
                data = Protect(data);
                File.WriteAllBytes(ConfigPath, data);
            }
            catch (Exception e)
            {
                Util.TraceException("Error while saving VpnData", e);
            }
        }

        private static void CreateNewSalt()
        {
            var salt = new byte[8];
            var rnd = new Random();
            rnd.NextBytes(salt);
            _salt = salt;
            File.WriteAllBytes(SaltPath, _salt);
        }
    }
}
