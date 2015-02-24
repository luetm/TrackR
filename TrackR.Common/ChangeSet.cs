using System.Collections.Generic;
using System.ComponentModel;

namespace TrackR.Common
{
    /// <summary>
    /// Represents a changeset that can be sent to the server.
    /// </summary>
    public class ChangeSet
    {
        public List<EntityWrapper> Entities { get; set; }
    }
}
