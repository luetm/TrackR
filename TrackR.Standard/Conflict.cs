using System;

namespace TrackR.Common
{
    public class Conflict
    {
        public object ServerVersion { get; set; }
        public object ClientVersion { get; set; }
        public  Type Type { get; set; }
    }
}
