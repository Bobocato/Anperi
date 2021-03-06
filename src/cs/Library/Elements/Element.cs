﻿namespace JJA.Anperi.Lib.Elements
{
    public abstract class Element
    {
        public int row { get; set; }
        public int column { get; set; }
        public int row_span { get; set; } = 1;
        public int column_span { get; set; } = 1;
        public float row_weight { get; set; } = 1.0f;
        public float column_weight { get; set; } = 1.0f;
        public abstract string type { get; }
        public string id { get; set; }
    }
}
