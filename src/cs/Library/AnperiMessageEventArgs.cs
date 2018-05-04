using System;
using JJA.Anperi.Lib.Message;

namespace JJA.Anperi.Lib
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
