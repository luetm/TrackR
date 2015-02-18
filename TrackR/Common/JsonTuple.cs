using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TrackR.Common
{
    /// <summary>
    /// Represents a property that changed.
    /// </summary>
    public class JsonTuple
    {
        /// <summary>
        /// Name of the property.
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// Full name of the type the property represents.
        /// </summary>
        public string PropertyType { get; set; }

        /// <summary>
        /// New value.
        /// </summary>
        public string JsonValue { get; set; }
    }
}
