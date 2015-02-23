using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrackR.Client;

namespace TrackR.OData
{
    public abstract class ODataTrackRContext : TrackRContext
    {
        protected ODataTrackRContext(Uri trackRUri) : base(trackRUri)
        {
        }
    }
}
