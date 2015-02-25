using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TrackR.OData.v3
{
    public class ODataContractResolver : DefaultContractResolver
    {
        protected override JsonObjectContract CreateObjectContract(Type objectType)
        {
            JsonObjectContract objectContract = base.CreateObjectContract(objectType);

            var properties = new List<JsonProperty>(objectContract.Properties);
            while (objectContract.Properties.Count > 0)
            {
                objectContract.Properties.RemoveAt(0);
            }

            objectContract.Properties.Add(new JsonProperty
            {
                PropertyName = "odata.type",
                PropertyType = typeof(string),
                ValueProvider = new StaticValueProvider(objectType.FullName),
                Readable = true,
            });

            foreach (var property in properties.OrderBy(p => p.PropertyName))
            {
                if (!property.Writable) continue;
                if (typeof(IEnumerable).IsAssignableFrom(property.PropertyType))
                {
                    //var value = property.ValueProvider.GetValue(objectContract.)
                }
                objectContract.Properties.Add(property);
            }

            return objectContract;
        }

        private class StaticValueProvider : IValueProvider
        {
            private readonly object _value;

            public StaticValueProvider(object value)
            {
                _value = value;
            }

            public object GetValue(object target)
            {
                return _value;
            }

            public void SetValue(object target, object value)
            {
                throw new NotSupportedException();
            }
        }
    }
}
