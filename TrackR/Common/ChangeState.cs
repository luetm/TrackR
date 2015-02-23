using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackR.Client
{
    /// <summary>
    /// State tracked of an entity.
    /// </summary>
    public enum ChangeState
    {
        Unchanged,
        Added,
        Deleted,
        Changed,
    }
}
