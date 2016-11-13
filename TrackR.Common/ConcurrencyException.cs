using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackR.Common
{
    public class ConcurrencyException : Exception
    {
        public ChangeSet ChangetSet { get; }
        public List<Conflict> Conflicts { get; }

        public ConcurrencyException(List<Conflict> conflicts, ChangeSet changetSet)
        {
            ChangetSet = changetSet;
            Conflicts = conflicts;
        }
    }
}
