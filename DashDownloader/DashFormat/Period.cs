using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DashDownloader.DashFormat
{
    public class Period
    {
        public int Id { get; }
        public AdaptationSet[] AdaptationSets { get; }

        public Period(int id, IEnumerable<AdaptationSet> adaptationSets)
        {
            Id = id;
            AdaptationSets = adaptationSets.ToArray();
        }
    }
}
