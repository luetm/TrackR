using System;
using System.Collections.Generic;

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
