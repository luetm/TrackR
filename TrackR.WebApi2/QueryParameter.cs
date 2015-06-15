using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackR.WebApi2
{
    public class QueryParameter
    {
        /// <summary>
        /// Uri path (relative from the base uri or absolute).
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// URI Parameters (new { name = variable.Name })
        /// </summary>
        public object UriParameters { get; set; }

        /// <summary>
        /// Key value store for the body (new { name = variable.Name })
        /// </summary>
        public object BodyKeyValueStore { get; set; }

        /// <summary>
        /// Raw body content (manual).
        /// </summary>
        public string BodyRaw { get; set; }
    }
}
