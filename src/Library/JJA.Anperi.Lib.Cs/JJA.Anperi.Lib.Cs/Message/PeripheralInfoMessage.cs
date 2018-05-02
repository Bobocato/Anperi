using System;
using System.Collections.Generic;
using System.Text;
using JJA.Anperi.Ipc.Dto;
using JJA.Anperi.Utility;

namespace JJA.Anperi.Lib.Cs.Message
{
    public class PeripheralInfoMessage : AnperiMessage
    {
        protected override IpcMessageCode MsgCode => IpcMessageCode.GetPeripheralInfo;

        public PeripheralInfoMessage(Dictionary<string, dynamic> data) : base(data)
        {
        }

        public enum ScreenType
        {
            generic, phone, tablet
        }

        public int Version
        {
            get => base.Data.TryGetValue("version", out int val) ? val : default(int);
            set => base.Data["version"] = value;
        }
        public int ScreenWidth
        {
            get => base.Data.TryGetValue("screen_width", out int val) ? val : default(int);
            set => base.Data["screen_width"] = value;
        }
        public int ScreenHeight
        {
            get => base.Data.TryGetValue("screen_height", out int val) ? val : default(int);
            set => base.Data["screen_height"] = value;
        }

        public ScreenType Screen
        {
            get
            {
                ScreenType res = ScreenType.generic;
                if (base.Data.TryGetValue("screen_type", out string st))
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
