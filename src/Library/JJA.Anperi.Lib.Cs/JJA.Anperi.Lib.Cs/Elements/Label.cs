using System;
using System.Collections.Generic;
using System.Text;

namespace JJA.Anperi.Lib.Cs.Elements
{
    public class Label : Element
    {
        public override string type => "label";
        public string text { get; set; }
    }
}
