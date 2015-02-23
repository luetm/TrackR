using System.Collections.Generic;
using System.Data.Entity;
using System.Reflection;
using TestData.Entities;
using TrackR.Server;

namespace TestSite.Controllers
{
    public class TrackRController : TrackRControllerBase
    {

        protected override DbContext CreateContext()
        {
            return new TestDbContext();
        }

        protected override void AddAssemblies(List<Assembly> assemblies)
        {
            base.AddAssemblies(assemblies);
            assemblies.Add(typeof(Patient).Assembly);
        }
    }
}
