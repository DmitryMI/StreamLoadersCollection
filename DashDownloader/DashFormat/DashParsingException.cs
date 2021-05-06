using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DashDownloader.DashFormat
{
    public class DashParsingException : Exception
    {
        public DashParsingException(string message) : base(message)
        {

        }
        public DashParsingException(string message, Exception inner) : base(message, inner)
        {

        }
    }
}
