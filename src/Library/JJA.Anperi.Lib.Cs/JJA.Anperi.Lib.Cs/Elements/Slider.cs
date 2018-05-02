﻿using System;
using System.Collections.Generic;
using System.Text;

namespace JJA.Anperi.Lib.Cs.Elements
{
    public class Slider : Element
    {
        public override string type => "slider";

        public int min { get; set; } = 0;
        public int max { get; set; } = 100;
        public int progress { get; set; } = 50;
        public int step_size { get; set; } = 2;
    }
}
