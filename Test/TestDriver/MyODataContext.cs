using System;
using System.ComponentModel;
using TrackR.OData.v3;
using Container = TestDriver.Service_References.TestSiteReference.Container;

namespace TestDriver
{
    public class MyODataContext : ODataTrackRContext<Container>
    {
        public MyODataContext()
            : base(new Uri("http://localhost.fiddler:3663/odata"), new Uri("http://localhost.fiddler:3663/api/TrackR"))
        {

        }

        protected override int GetId(INotifyPropertyChanged entity)
        {
            return (int)entity.GetType().GetProperty("Id").GetValue(entity);
        }
    }
}
