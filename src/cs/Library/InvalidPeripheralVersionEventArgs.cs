using System;
using System.Collections.Generic;
using System.Text;

namespace JJA.Anperi.Lib
{
    public class InvalidPeripheralVersionEventArgs : EventArgs
    {
        public int PeripheralVersion { get; }
        public int LibVerifiedVersion { get; }

        public InvalidPeripheralVersionEventArgs(int peripheralVersion, int libVerifiedVersion)
        {
            PeripheralVersion = peripheralVersion;
            LibVerifiedVersion = libVerifiedVersion;
        }
    }
}
