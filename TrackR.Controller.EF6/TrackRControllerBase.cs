using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using TrackR.Common;

namespace TrackR.Controller.EF6
{
    public abstract class TrackRControllerBase : ApiController
    {
        private readonly List<Assembly> _assemblies;

        protected TrackRControllerBase()
        {
            // ReSharper disable once DoNotCallOverridableMethodsInConstructor
            _assemblies = new List<Assembly>();

            // ReSharper disable once DoNotCallOverridableMethodsInConstructor
            AddAssemblies(_assemblies);
        }

        protected virtual void OnPosting(ChangeSet cs) { }
        protected virtual void OnPosted(string json) { }
        protected virtual void OnException(Exception err) { }
        protected virtual HttpResponseMessage OnOptimisticConcurrencyException(DbContext context, DbUpdateConcurrencyException err)
        {
            return Request.CreateErrorResponse(HttpStatusCode.Conflict, err);
        }

        public async Task<HttpResponseMessage> Post(ChangeSet changeSet)
        {
            using (var context = CreateContext())
            {
                try
                {
                    if (changeSet == null)
                    {
                        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "");
                    }

                    OnPosting(changeSet);

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
                        context.Entry(remove.Entity).State = EntityState.Deleted;
                    }

                    // Add flagged entities
                    var toAdd = changeSet.Entities
                        .Where(e => e.ChangeState == ChangeState.Added)
                        .ToList();
                    foreach (var add in toAdd)
                    {
                        context.Entry(add.Entity).State = EntityState.Added;
                    }

                    // Modify flagged entities
                    var toEdit = changeSet.Entities
                        .Where(e => e.ChangeState == ChangeState.Changed)
                        .ToList();
                    foreach (var edit in toEdit)
                    {
                        context.Entry(edit.Entity).State = EntityState.Modified;
                    }

                    // Save changes
                    await context.SaveChangesAsync();

                    // Update entities
                    foreach (var wrapper in changeSet.Entities.ToList())
                    {
                        if (wrapper.ChangeState == ChangeState.Deleted)
                        {
                            continue;
                        }

                        context.Entry(wrapper.Entity).Reload();
                    }

                    var settings = new JsonSerializerSettings 
                    {
                        ContractResolver = new FlatJsonResolver(),
                        TypeNameHandling = TypeNameHandling.Objects,
                        PreserveReferencesHandling = PreserveReferencesHandling.All,
                        ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                        MaxDepth = 100,
                        Culture = CultureInfo.InvariantCulture,
                        StringEscapeHandling = StringEscapeHandling.Default,
                    };
                    var json = JsonConvert.SerializeObject(new ChangeSet { Entities = changeSet.Entities.Where(w => w.ChangeState != ChangeState.Deleted).ToList() }, settings);
                    OnPosted(json);

                    var response = Request.CreateResponse(HttpStatusCode.OK);
                    response.Content = new StringContent(json, Encoding.UTF8, "application/json");
                    OnSuccess(changeSet);
                    return response;
                }
                catch (DbUpdateConcurrencyException err)
                {
                    return OnOptimisticConcurrencyException(context, err);
                }
                catch (Exception err)
                {
                    OnException(err);
                    return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, err.GenerateInfo());
                }
            }
        }

        protected abstract DbContext CreateContext();

        protected virtual void AddAssemblies(List<Assembly> assemblies)
        {
            _assemblies.Add(typeof(string).Assembly);
        }

        protected virtual void OnSuccess(ChangeSet changeset) { }
        
        private void Reconstruct(EntityWrapper wrapper, List<EntityWrapper> entities)
        {
            foreach (var reference in wrapper.References)
            {
                // The reference 
                var refWrapper = entities.FirstOrDefault(e => e.Guid == reference.Reference);

                // This reference must have been unchanged or we're not interested in it due to deletion
                if (refWrapper == null || refWrapper.ChangeState == ChangeState.Deleted || wrapper.ChangeState == ChangeState.Deleted)
                    continue;

                // Attach
                var property = wrapper.Entity.GetType().GetProperty(reference.PropertyName);
                if (wrapper.ChangeState == ChangeState.Deleted)
                {
                    if (!typeof(IEnumerable).IsAssignableFrom(property.PropertyType))
                    {
                        property.SetValue(wrapper.Entity, null);
                    }
                }
                else
                {
                    if (typeof(IEnumerable).IsAssignableFrom(property.PropertyType) && !property.PropertyType.IsArray)
                    {
                        continue;
                    }
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
