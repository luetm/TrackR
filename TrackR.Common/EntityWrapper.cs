using System;
using System.Collections.Generic;

namespace TrackR.Common
{
    public class EntityWrapper
    {
        public Guid Guid { get; set; }
        public object Entity { get; set; }
        public ChangeState ChangeState { get; set; }
        public List<EntityReference> References { get; set; }

        public string ChangeLog { get; set; }
    }
}
