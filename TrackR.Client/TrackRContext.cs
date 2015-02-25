using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Remoting;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TrackR.Common;

namespace TrackR.Client
{
    /// <summary>
    /// Base class of specific (webapi, odata, ...) TrackR contexts.
    /// </summary>
    public abstract class TrackRContext
    {
        /// <summary>
        /// Base uri of the context.
        /// </summary>
        public Uri BaseUri { get; protected set; }

        /// <summary>
        /// Entity sets contained in this context.
        /// </summary>
        public List<EntitySet> EntitySets { get; private set; }

        /// <summary>
        /// Returns true if there were any changes made in the context.
        /// </summary>
        /// <returns></returns>
        public bool HasChanges
        {
            get
            {
                return EntitySets
                    .SelectMany(e => e.EntitiesNonGeneric)
                    .Any(e => e.State != ChangeState.Unchanged);
            }
        }


        /// <summary>
        /// URI to the TrackR Controller.
        /// </summary>
        protected readonly Uri TrackRUri;


        /// <summary>
        /// Base constructor.
        /// </summary>
        /// <param name="trackRUri"></param>
        protected TrackRContext(Uri trackRUri)
        {
            TrackRUri = trackRUri;
            BaseUri = new UriBuilder(trackRUri) { Path = "", Query = "" }.Uri;
            EntitySets = new List<EntitySet>();
        }


        /// <summary>
        /// Adds an entity to the context. Set will be determined automatically.
        /// </summary>
        /// <param name="entity"></param>
        public void Add(INotifyPropertyChanged entity)
        {
            var entitySet = GetEntitySet(entity);
            entitySet.AddEntity(entity);
        }

        /// <summary>
        /// Removes an entity from the context. Set will be determined automatically.
        /// </summary>
        /// <param name="entity"></param>
        public void Remove(INotifyPropertyChanged entity)
        {
            var entitySet = GetEntitySet(entity);
            entitySet.RemoveEntity(entity);
        }

        /// <summary>
        /// Tracks an entity (attach).
        /// </summary>
        /// <param name="entity"></param>
        public void Track(INotifyPropertyChanged entity)
        {
            var entitySet = GetEntitySet(entity);
            entitySet.TrackEntity(entity);

            var properties = entity.GetType().GetProperties();
            foreach (var property in properties)
            {
                if (property.PropertyType.IsValueType || property.PropertyType == typeof(string))
                    continue;

                var value = property.GetValue(entity);
                if (value == null)
                    continue;

                if (value is IEnumerable)
                {
                    foreach (var v in value as IEnumerable)
                    {
                        if (v is INotifyPropertyChanged)
                        {
                            Track(v as INotifyPropertyChanged);
                        }
                    }
                }
                else
                {
                    var v = property.GetValue(entity);
                    if (v is INotifyPropertyChanged)
                    {
                        Track(v as INotifyPropertyChanged);
                    }
                }
            }
        }

        /// <summary>
        /// Tracks all entities in the collection.
        /// </summary>
        /// <param name="collection"></param>
        public void TrackMany(IEnumerable<INotifyPropertyChanged> collection)
        {
            foreach (var entity in collection)
            {
                Track(entity);
            }
        }

        /// <summary>
        /// Untracks an entity (detach).
        /// </summary>
        /// <param name="entity"></param>
        public void UnTrack(INotifyPropertyChanged entity)
        {
            var entitySet = GetEntitySet(entity);
            entitySet.UnTrackEntity(entity);
        }


        /// <summary>
        /// Submits all changes.
        /// </summary>
        public async Task SubmitChangesAsync()
        {
            try
            {
                var changetSet = BuildChangeSet();

                var settings = new JsonSerializerSettings
                {
                    ContractResolver = new FlatJsonResolver(),
                    TypeNameHandling = TypeNameHandling.Objects,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
#if DEBUG
                    Formatting = Formatting.Indented,
#endif
                };
                var json = JsonConvert.SerializeObject(changetSet, settings);

                using (var client = CreateHttpClient())
                {
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    var result = await client.PostAsync(TrackRUri, new StringContent(json, Encoding.UTF8, "application/json"));
                    var content = await result.Content.ReadAsStringAsync();

                    if (!result.IsSuccessStatusCode)
                    {
                        throw new ServerException("Server returned: {0}\n{1}".FormatStatic(result.StatusCode, content));
                    }
                }

                foreach (var wrapper in changetSet.Entities)
                {
                    if (wrapper.ChangeState == ChangeState.Deleted)
                    {
                        Remove(wrapper.Entity as INotifyPropertyChanged);
                    }
                    else
                    {
                        wrapper.ChangeState = ChangeState.Unchanged;
                    }
                }
            }
            catch (Exception e)
            {
                throw new WebException("Could not save changes. See inner exception for details.", e);
            }
        }

        /// <summary>
        /// Rejects all changes and reverts to original.
        /// </summary>
        public void RejectChanges()
        {
            foreach (var tracker in EntitySets.SelectMany(s => s.EntitiesNonGeneric).ToList())
            {
                if (tracker.State == ChangeState.Unchanged)
                    continue;

                if (tracker.State == ChangeState.Added)
                {
                    Remove(tracker.GetEntity());
                }

                if (tracker.State == ChangeState.Deleted)
                {
                    tracker.State = ChangeState.Changed;
                }

                if (tracker.State == ChangeState.Changed)
                {
                    tracker.RevertToOriginal();
                }
            }
        }

        /// <summary>
        /// Clears all changes.
        /// </summary>
        public virtual void Clear()
        {
            foreach (var set in EntitySets)
            {
                set.Clear();
            }
            EntitySets.Clear();
        }


        /// <summary>
        /// Compiles all changes from tracked entities.
        /// </summary>
        /// <returns></returns>
        private ChangeSet BuildChangeSet()
        {
            var entities = EntitySets.SelectMany(s => s.EntitiesNonGeneric)
                .Where(e => e.State == ChangeState.Changed || e.State == ChangeState.Added || e.State == ChangeState.Deleted)
                .ToList();

            var allEntities = EntitySets.SelectMany(s => s.EntitiesNonGeneric).ToList();

            var wrappers = new List<EntityWrapper>();
            foreach (var entity in entities)
            {
                BuildWrapper(entity, wrappers, allEntities);
            }

            return new ChangeSet
            {
                Entities = wrappers.Where(w => w.ChangeState != ChangeState.Unchanged).ToList(),
            };
        }

        /// <summary>
        /// Builds a wrapper from entities (RECURSIVE)
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="wrappers"></param>
        /// <param name="allEntities"></param>
        private void BuildWrapper(EntityTracker entity, List<EntityWrapper> wrappers, List<EntityTracker> allEntities)
        {
            if (wrappers.Any(e => e.Guid == entity.Guid))
                return;

            var wrapper = new EntityWrapper
            {
                Entity = entity.GetEntity(),
                Guid = entity.Guid,
                ChangeState = entity.State,
                References = new List<EntityReference>()
            };

            wrappers.Add(wrapper);

            var properties = entity.GetEntity().GetType().GetProperties()
                .Where(p => !p.PropertyType.IsValueType)
                .Where(p => p.PropertyType != typeof(string))
                .Where(p => p.CanRead && p.CanWrite);

            foreach (var property in properties)
            {
                var value = property.GetValue(entity.GetEntity());
                if (value is IEnumerable)
                {
                    foreach (var v in value as IEnumerable)
                    {
                        if (allEntities.Any(e => Equals(e.GetEntity(), v)))
                        {
                            var tracker = allEntities.First(t => Equals(t.GetEntity(), v));
                            wrapper.References.Add(new EntityReference
                            {
                                PropertyName = property.Name,
                                Reference = tracker.Guid,
                            });
                            BuildWrapper(tracker, wrappers, allEntities);
                        }
                    }
                }
                else if (allEntities.Any(e => Equals(e.GetEntity(), value)))
                {
                    if (allEntities.Any(e => Equals(e.GetEntity(), value)))
                    {
                        var tracker = allEntities.First(t => Equals(t.GetEntity(), value));
                        wrapper.References.Add(new EntityReference
                        {
                            PropertyName = property.Name,
                            Reference = tracker.Guid,
                        });
                        BuildWrapper(tracker, wrappers, allEntities);
                    }
                }
            }
        }

        /// <summary>
        /// Gets an entity set based on an entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        private EntitySet GetEntitySet(INotifyPropertyChanged entity)
        {
            if (entity == null)
                throw new ArgumentNullException("entity");

            var entitySet = EntitySets.SingleOrDefault(s => s.Type == entity.GetType().FullName);
            if (entitySet == null)
            {
                var newSetType = typeof(EntitySet<>).MakeGenericType(entity.GetType());
                var newSet = (EntitySet)Activator.CreateInstance(newSetType);
                EntitySets.Add(newSet);
                return newSet;
            }

            return entitySet;
        }

        /// <summary>
        /// Gets an ID of an entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected abstract int GetId(INotifyPropertyChanged entity);


        /// <summary>
        /// For unit testing.
        /// </summary>
        /// <returns></returns>
        protected virtual HttpClient CreateHttpClient()
        {
            return new HttpClient();
        }
    }
}
