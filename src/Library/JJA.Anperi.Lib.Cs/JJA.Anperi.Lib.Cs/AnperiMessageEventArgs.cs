using System;
using JJA.Anperi.Lib.Cs.Message;

namespace JJA.Anperi.Lib.Cs
{
    public class AnperiMessageEventArgs : EventArgs
    {
        public AnperiMessage Message { get; set; }

        public AnperiMessageEventArgs(AnperiMessage am)
        {
            Message = am;
        }
    }
}
