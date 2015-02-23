using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrackR.OData;
using TrackR.OData.v3;

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

        protected override int GetId(INotifyPropertyChanged entity)
        {
            return (int)entity.GetType().GetProperty("Id").GetValue(entity);
        }
    }
}
