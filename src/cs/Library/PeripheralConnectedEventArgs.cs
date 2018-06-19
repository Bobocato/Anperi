using System;
using System.Collections.Generic;
using System.Text;
using JJA.Anperi.Lib.Message;

namespace JJA.Anperi.Lib
{
    public class PeripheralConnectedEventArgs : EventArgs
    {
        public PeripheralConnectedEventArgs(PeripheralInfo peripheralInfo)
        {
            PeripheralInfo = peripheralInfo;
        }

        public PeripheralInfo PeripheralInfo { get; private set; }
    }
}
