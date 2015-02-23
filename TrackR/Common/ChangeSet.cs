using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrackR.Client;

namespace TrackR.Common
{
    /// <summary>
    /// Represents a changeset that can be sent to the server.
    /// </summary>
    public class ChangeSet
    {
        /// <summary>
        /// Entities that should be added.
        /// </summary>
        public List<INotifyPropertyChanged> ToAdd { get; set; }

        /// <summary>
        /// Entity-parts that should be edited.
        /// </summary>
        public List<JsonPropertySet> ToEdit { get; set; }

        /// <summary>
        /// Entity-Ids for the entities that should be deleted.
        /// </summary>
        public List<JsonIdReference> ToDelete { get; set; }
    }
}
