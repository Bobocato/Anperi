using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnperiRemote.Model
{
    class AnperiVolumeEventArgs : EventArgs
    {
        public string ElementId { get; }
        public int Volume { get; }
        public AnperiVolumeEventArgs(string elementId, int volume)
        {
            ElementId = elementId;
            Volume = volume;
        }
    }

    class VolumeEventArgs : EventArgs
    {
        public int Volume { get; }
        public VolumeEventArgs(int volume)
        {
            Volume = volume;
        }
    }
}
