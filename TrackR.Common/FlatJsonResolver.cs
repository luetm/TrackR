using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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

            if (prop.DeclaringType != typeof(ChangeSet) &&
                prop.DeclaringType != typeof(EntityWrapper) &&
                prop.PropertyType != typeof(string) &&
                (prop.PropertyType.IsClass || prop.PropertyType.IsInterface) &&
                !prop.PropertyType.IsArray)
            {
                prop.ShouldSerialize = obj => false;
            }

            if (typeof(List<int>).IsArray)
            {
                Debugger.Break();
            }

            return prop;
        }
    }
}
