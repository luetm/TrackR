using System;
using TrackR.Client;

namespace TrackR.OData.v3
{
    public abstract class ODataTrackRContext : TrackRContext
    {
        protected ODataTrackRContext(Uri trackRUri) : base(trackRUri)
        {
        }
    }
}
