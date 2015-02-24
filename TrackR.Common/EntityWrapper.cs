using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackR.Common
{
    public class EntityWrapper
    {
        public Guid Guid { get; set; }
        public object Entity { get; set; }
        public ChangeState ChangeState { get; set; }
        public List<EntityReference> References { get; set; }
    }
}
