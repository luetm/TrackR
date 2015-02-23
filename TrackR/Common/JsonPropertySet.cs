using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TrackR.Common
{
    /// <summary>
    /// Contains all properties that changed for an entity, as well as the required identification information (type and id) for that entity.
    /// </summary>
    public class JsonPropertySet
    {
        /// <summary>
        /// Fully qualified entity type.
        /// </summary>
        public string EntityType { get; set; }

        /// <summary>
        /// Id of the entity to be edited.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// All properties that changed (for avoiding sending entire trees unnecessarily.
        /// </summary>
        public List<JsonTuple> ChangedProperties { get; set; }
    }
}
