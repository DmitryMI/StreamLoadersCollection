using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DashDownloader.DashFormat
{
    public class Mpd
    {
        public Period[] Periods { get; }

        public Mpd(IEnumerable<Period> periods)
        {
            Periods = periods.ToArray();
        }
    }
}
