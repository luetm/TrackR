using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace TrackR.Common
{
    public class FlatJsonResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty prop = base.CreateProperty(member, memberSerialization);

            // Property has to be readable and writable.
            if (!(prop.Writable && prop.Readable))
                prop.ShouldSerialize = obj => false;
            
            // JsonIgnore attribute
            if (prop.AttributeProvider.GetAttributes(typeof (JsonIgnoreAttribute), true).Any())
                prop.ShouldSerialize = obj => false;


            if (prop.DeclaringType != typeof(ChangeSet) &&
                prop.DeclaringType != typeof(EntityWrapper) &&
                prop.PropertyType != typeof(string) &&
                (prop.PropertyType.IsClass || prop.PropertyType.IsInterface) &&
                !prop.PropertyType.IsArray)
            {
                prop.ShouldSerialize = obj => false;
            }

            return prop;
        }
    }
}
