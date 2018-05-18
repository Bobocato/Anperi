using System;
using System.Collections.Generic;
using JJA.Anperi.Ipc.Dto;
using JJA.Anperi.Utility;

namespace JJA.Anperi.Lib.Message
{
    public class EventFiredAnperiMessage : AnperiMessage
    {
        protected override IpcMessageCode MsgCode => IpcMessageCode.PeripheralEventFired;

        public EventFiredAnperiMessage(Dictionary<string, dynamic> data) : base(data)
        {
        }

        public enum EventType
        {
            on_click, on_click_long, on_change, on_input
        }

        public EventType Event
        {
            get
            {
                EventType res = EventType.on_click;
                if (base.Data.TryGetValue("type", out string st))
                {
                    if (Enum.TryParse(st, out EventType ste))
                    {
                        res = ste;
                    }
                }
                return res;
            }
        }

        public string ElementId
        {
            get => base.Data.TryGetValue("id", out string val) ? val : default(string);
            set => base.Data["id"] = value;
        }

        public Dictionary<string, dynamic> EventData
        {
            get
            {
                base.Data.TryGetValue("data", out Dictionary<string, dynamic> val);
                return val;
            }
        }
    }
}
