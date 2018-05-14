using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace JJA.Anperi.Lib.Elements
{
    public class CheckBox : Element
    {
        public override string type => "checkbox";

        /// <summary>
        /// This should actually be lowercase but since that's a keyword in c# this has to do.
        /// </summary>
        [JsonProperty("checked")]
        public bool Checked { get; set; }
    }
}
