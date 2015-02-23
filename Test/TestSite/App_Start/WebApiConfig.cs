using Newtonsoft.Json;
using System.Linq;
using System.Net.Http.Formatting;
using System.Web.Http;
using System.Web.Http.OData.Builder;
using System.Web.Http.OData.Extensions;
using TestData.Entities;
    

namespace TestSite
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services
            var json = config.Formatters.OfType<JsonMediaTypeFormatter>().First();
            json.SerializerSettings.TypeNameHandling = TypeNameHandling.Objects;

            // Web API routes
            config.MapHttpAttributeRoutes();

            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<Patient>("Patients");
            builder.EntitySet<Address>("Addresses");
            builder.EntitySet<Associate>("Associates");
            builder.EntitySet<Insurance>("Insurances");
            builder.EntitySet<PatientInsurance>("PatientInsurances");
            config.Routes.MapODataServiceRoute("odata", "odata", builder.GetEdmModel());

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }
    }
}
