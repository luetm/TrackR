using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using TrackR.Common;

namespace TrackR.Client
{
    public class FlatJsonResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty prop = base.CreateProperty(member, memberSerialization);

            if (prop.PropertyType != typeof(List<EntityWrapper>) &&
                prop.DeclaringType != typeof(EntityWrapper) &&
                !prop.PropertyType.IsArray &&
                prop.PropertyType.IsClass &&
                prop.PropertyType != typeof(string))
            {
                prop.ShouldSerialize = obj => false;
            }

            return prop;
        }
    }
}
