using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DashDownloader.DashFormat
{
    public class AdaptationSet
    {
        public int Id { get; }
        public string Lang { get; set; }
        public string ContentType { get; set; }

        public int MaxHeight { get; set; }
        public int MaxWidth { get; set; }

        public Representation[] Representations { get;}

        public AdaptationSet(int id, string lang, string contentType, int maxHeight, int maxWidth,IEnumerable<Representation> representations)
        {
            Id = id;
            Lang = lang;
            ContentType = contentType;
            MaxHeight = maxHeight;
            MaxWidth = maxWidth;
            Representations = representations.ToArray();
        }
    }
}
