using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DashDownloader.DashFormat
{
    public class SegmentTemplate
    {
        public string Initialization { get;  }
        public string Media { get;  }
        public int StartNumber { get;  }
        public int Timescale { get; }

        SegmentTimeline SegmentTimeline { get; }

        public SegmentTemplate(string initialization, string media, int startNumber, int timescale, SegmentTimeline segmentTimeline)
        {
            Initialization = initialization;
            Media = media;
            StartNumber = startNumber;
            Timescale = timescale;
            SegmentTimeline = segmentTimeline;
        }
    }
}
