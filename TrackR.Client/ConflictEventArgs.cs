using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrackR.Common;

namespace TrackR.Client
{
    public class ConflictEventArgsEventArgs : EventArgs
    {
        public IEnumerable<Conflict> Conflicts { get; }
        public bool IsHandleled { get; private set; }

        public ConflictEventArgsEventArgs(IEnumerable<Conflict> conflicts)
        {
            Conflicts = conflicts;
        }

        public void SetHandeled()
        {
            IsHandleled = true;
        }
    }
}
