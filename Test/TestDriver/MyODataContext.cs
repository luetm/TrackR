using System;
using System.ComponentModel;
using TestData.Entities;
using TrackR.OData.v3;
using Container = TestDriver.Service_References.TestSiteReference.Container;

namespace TestDriver
{
    public class MyODataContext : ODataTrackRContext<Container, Entity>
    {
        public MyODataContext()
            : base(new Uri("http://localhost.fiddler:3663/odata"), new Uri("http://localhost.fiddler:3663/api/TrackR"))
        {

        }

        protected override int GetId(object entity)
        {
            return (int)entity.GetType().GetProperty("Id").GetValue(entity);
        }

        protected override void SetId(object entity, int value)
        {
            entity.GetType().GetProperty("Id").SetValue(entity, value);
        }
    }
}
