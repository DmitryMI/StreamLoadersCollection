using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DashDownloader.DashFormat
{
    public class Representation
    {
        public int Id { get; }
        public string MimeType { get; }
        public int Bandwidth { get;}
        public int Width { get;}
        public int Height { get;  }

        public SegmentTemplate SegmentTemplate {get;}

        public Representation(int id, string mimeType, int bandwidth, int width, int height, SegmentTemplate segmentTemplate)
        {
            Id = id;
            MimeType = mimeType;
            Bandwidth = bandwidth;
            Width = width;
            Height = height;
            SegmentTemplate = segmentTemplate;
        }
    }
}
