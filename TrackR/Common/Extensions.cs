using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackR.Common
{
    public static class Extensions
    {
        public static string F(this string format, params object[] args)
        {
            return string.Format(format, args);
        }

        public static bool IsNullOrWhiteSpace(this string input)
        {
            return string.IsNullOrWhiteSpace(input);
        }
    }
}
