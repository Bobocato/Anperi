using System;
using System.Collections.Generic;
using System.Text;

namespace JJA.Anperi.Lib.Cs.Elements
{
    public class Button : Element
    {
        public override string type => "button";
        public string text { get; set; }
    }
}
