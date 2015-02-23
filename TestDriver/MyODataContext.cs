using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrackR.OData;

namespace TestDriver
{
    public class MyODataContext : ODataTrackRContext
    {
        public MyODataContext()
            : this(new Uri("http://localhost.fiddler:3663/api/TrackR"))
        {

        }

        private MyODataContext(Uri trackRUri)
            : base(trackRUri)
        {
        }

        protected override int GetId(object entity)
        {
            return (int)entity.GetType().GetProperty("Id").GetValue(entity);
        }
    }
}
