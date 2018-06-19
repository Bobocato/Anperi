using System;
using System.Collections.Generic;
using JJA.Anperi.Ipc.Dto;
using JJA.Anperi.Utility;

namespace JJA.Anperi.Lib.Message
{
    public class PeripheralInfo
    {
        public Dictionary<string, dynamic> RawData { get; set; }

        public PeripheralInfo(Dictionary<string, dynamic> data)
        {
            RawData = data;
        }

        public enum ScreenType
        {
            generic, phone, tablet
        }

        public int Version
        {
            get => RawData.TryGetValue("version", out int val) ? val : default(int);
            set => RawData["version"] = value;
        }
        public int ScreenWidth
        {
            get => RawData.TryGetValue("screen_width", out int val) ? val : default(int);
            set => RawData["screen_width"] = value;
        }
        public int ScreenHeight
        {
            get => RawData.TryGetValue("screen_height", out int val) ? val : default(int);
            set => RawData["screen_height"] = value;
        }

        public ScreenType Screen
        {
            get
            {
                ScreenType res = ScreenType.generic;
                if (RawData.TryGetValue("screen_type", out string st))
                {
                    if (Enum.TryParse(st, out ScreenType ste))
                    {
                        res = ste;
                    }
                }
                return res;
            }
        }
    }
}
