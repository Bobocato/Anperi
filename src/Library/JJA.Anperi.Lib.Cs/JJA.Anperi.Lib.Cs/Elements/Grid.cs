using System;
using System.Collections.Generic;
using System.Text;

namespace JJA.Anperi.Lib.Cs.Elements
{
    public class Grid : Element
    {
        public override string type => "sub-grid";

        public List<Element> elements { get; set; } = new List<Element>();
    }
}
