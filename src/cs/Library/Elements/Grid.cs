using System.Collections.Generic;

namespace JJA.Anperi.Lib.Elements
{
    public class Grid : Element
    {
        public override string type => "sub-grid";

        public List<Element> elements { get; set; } = new List<Element>();
    }
}
