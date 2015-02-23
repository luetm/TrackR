using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Web.Http;
using System.Web.Http.Results;
using Newtonsoft.Json;
using TrackR.Common;

namespace TrackR.Server
{
    public abstract class TrackRControllerBase : ApiController
    {
        private readonly DbContext _context;
        private readonly List<Assembly> _assemblies;

        protected TrackRControllerBase()
        {
            // ReSharper disable once DoNotCallOverridableMethodsInConstructor
            _context = CreateContext();
            _assemblies = new List<Assembly>();

            // ReSharper disable once DoNotCallOverridableMethodsInConstructor
            AddAssemblies(_assemblies);
        }

        protected abstract DbContext CreateContext();

        protected virtual void AddAssemblies(List<Assembly> assemblies)
        {
            _assemblies.Add(typeof(string).Assembly);
        }


        public IHttpActionResult Post(ChangeSet changeSet)
        {
            if (changeSet == null)
            {
                return BadRequest();
            }

            if (changeSet.ToAdd != null)
            {
                foreach (var add in changeSet.ToAdd)
                {
                    ApplyState(add, EntityState.Detached);
                }
            }

            if (changeSet.ToEdit != null)
            {
                foreach (var edit in changeSet.ToEdit)
                {
                    var type = ResolveType(edit.EntityType);
                    var set = _context.Set(type);
                    var entity = set.Find(edit.Id);

                    foreach (var changedProperty in edit.ChangedProperties)
                    {
                        var prop = entity.GetType().GetProperty(changedProperty.PropertyName);
                        var propertyType = ResolveType(changedProperty.PropertyType);
                        prop.SetValue(entity, JsonConvert.DeserializeObject(changedProperty.JsonValue, propertyType));
                    }

                    _context.Entry(entity).State = EntityState.Modified;
                }
            }

            if (changeSet.ToDelete != null)
            {
                foreach (var removeRef in changeSet.ToDelete)
                {
                    var type = ResolveType(removeRef.Type);
                    var set = _context.Set(type);
                    var entity = set.Find(removeRef.Id);
                    if (entity == null)
                    {
                        var message = new HttpResponseMessage(HttpStatusCode.Gone);
                        message.Content = new StringContent("{0} ({1})".F(removeRef.Type, removeRef.Id));
                        return new ResponseMessageResult(message);
                    }

                    _context.Entry(entity).State = EntityState.Deleted;
                }
            }
            _context.SaveChanges();

            return Ok();
        }

        private void ApplyState(object o, EntityState idNonZeroState, List<object> processed = null)
        {
            if (processed == null)
            {
                processed = new List<object>();
            }

            if (processed.Contains(o))
                return;

            processed.Add(o);

            if (o == null) return;

            var idProp = o.GetType().GetProperty("Id");
            if (idProp == null) return;

            if ((int)idProp.GetValue(o) == 0)
            {
                _context.Entry(o).State = EntityState.Added;
            }
            else
            {
                _context.Entry(o).State = idNonZeroState;
            }

            foreach (var prop in o.GetType().GetProperties())
            {
                if (!prop.PropertyType.IsValueType && prop.PropertyType != typeof(string))
                {
                    var value = prop.GetValue(o);
                    if (value == null) continue;

                    if (value is IEnumerable)
                    {
                        foreach (var v in value as IEnumerable)
                        {
                            ApplyState(v, idNonZeroState, processed);
                        }
                    }
                    else
                    {
                        ApplyState(value, idNonZeroState, processed);
                    }
                }
            }
        }

        private Type ResolveType(string fullType)
        {
            foreach (var a in _assemblies)
            {
                var type = a.GetType(fullType);
                if (type != null) return type;
            }

            throw new TypeLoadException("Could not find type {0}.".F(fullType));
        }
    }
}
