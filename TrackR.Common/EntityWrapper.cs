using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrackR2.Core.Data;

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
