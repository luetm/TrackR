using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace TrackR.WebApi2
{
    public class JsonObservableCollectionConverter : DefaultContractResolver
    {
        public JsonObservableCollectionConverter(bool shareCache) : base(shareCache)
        {

        }

        public override JsonContract ResolveContract(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ICollection<>))
            {
                return ResolveContract(typeof(ObservableCollection<>).MakeGenericType(type.GetGenericArguments()));
            }
            return base.ResolveContract(type);
        }
    }
}
