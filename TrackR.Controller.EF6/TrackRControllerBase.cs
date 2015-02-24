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
using TrackR.Common;

namespace TrackR.Controller.EF6
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

            // Delete flagged entities
            var toRemove = changeSet.Entities
                .Where(e => e.ChangeState == ChangeState.Deleted)
                .ToList();
            foreach (var remove in toRemove)
            {
                _context.Entry(remove.Entity).State = EntityState.Deleted;
            }
            
            // Add flagged entities
            var toAdd = changeSet.Entities
                .Where(e => e.ChangeState == ChangeState.Added)
                .ToList();
            foreach (var add in toAdd)
            {
                _context.Entry(add.Entity).State = EntityState.Added;
            }

            // Modify flagged entities
            var toEdit = changeSet.Entities
                .Where(e => e.ChangeState == ChangeState.Changed)
                .ToList();
            foreach (var edit in toEdit)
            {
                _context.Entry(edit.Entity).State = EntityState.Modified;
            }

            // Reconstruct object graphs
            foreach (var wrapper in changeSet.Entities)
            {
                Reconstruct(wrapper, changeSet.Entities);
            }

            // Save changes
            _context.SaveChanges();

            return Ok();
        }

        private void Reconstruct(EntityWrapper wrapper, List<EntityWrapper> entities)
        {
            foreach (var reference in wrapper.References)
            {
                var refWrapper = entities.FirstOrDefault(e => e.Guid == reference.Reference);
                
                // This reference must have been unchanged
                if (refWrapper == null) 
                    continue;

                // Attach
                var property = wrapper.Entity.GetType().GetProperty(reference.PropertyName);
                if (wrapper.ChangeState == ChangeState.Deleted)
                {
                    property.SetValue(wrapper.Entity, null);
                }
                else
                {
                    property.SetValue(wrapper.Entity, refWrapper.Entity);
                }
            }
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
