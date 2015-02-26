using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Reflection;
using System.Web.Http;
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

        public IHttpActionResult Post(ChangeSet changeSet)
        {
            if (changeSet == null)
            {
                return BadRequest();
            }

            // Reconstruct object graphs
            foreach (var wrapper in changeSet.Entities)
            {
                Reconstruct(wrapper, changeSet.Entities);
            }

            // Fix collections
            foreach (var wrapper in changeSet.Entities)
            {
                FixCollections(wrapper);
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

            // Save changes
            _context.SaveChanges();

            // Update entities
            foreach (var wrapper in changeSet.Entities.ToList())
            {
                if (wrapper.ChangeState == ChangeState.Deleted)
                {
                    changeSet.Entities.Remove(wrapper);
                    continue;
                }

                _context.Entry(wrapper.Entity).Reload();
            }


            var settings = new JsonSerializerSettings
            {
                ContractResolver = new FlatJsonResolver(),
                TypeNameHandling = TypeNameHandling.Objects,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            };
            var json = JsonConvert.SerializeObject(changeSet, settings);
            return Ok(json);
        }


        protected abstract DbContext CreateContext();

        protected virtual void AddAssemblies(List<Assembly> assemblies)
        {
            _assemblies.Add(typeof(string).Assembly);
        }


        private void Reconstruct(EntityWrapper wrapper, List<EntityWrapper> entities)
        {
            foreach (var reference in wrapper.References)
            {
                var refWrapper = entities.FirstOrDefault(e => e.Guid == reference.Reference);

                // This reference must have been unchanged
                if (refWrapper == null || refWrapper.ChangeState == ChangeState.Deleted || wrapper.ChangeState == ChangeState.Deleted)
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

        private void FixCollections(EntityWrapper wrapper)
        {
            var type = wrapper.Entity.GetType();
            foreach (var p in type.GetProperties())
            {
                if (p.CanRead && p.CanWrite && typeof(ICollection).IsAssignableFrom(p.PropertyType) && !p.PropertyType.IsArray)
                {
                    var collection = (IEnumerable)p.GetValue(wrapper.Entity);
                    if (collection != null && !collection.Cast<object>().Any())
                    {
                        p.SetValue(wrapper.Entity, null);
                    }
                }
            }
        }
    }
}
