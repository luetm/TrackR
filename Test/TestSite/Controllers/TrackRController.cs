using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Web.Http;
using System.Web.Http.Results;
using Newtonsoft.Json;
using TestData;
using TrackR.Common;
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
