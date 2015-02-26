using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Remoting;
using System.Text;
using System.Threading.Tasks;
using Omu.ValueInjecter;
using TrackR.Common;

namespace TrackR.Client
{
    /// <summary>
    /// Base class of specific (webapi, odata, ...) TrackR contexts.
    /// </summary>
    public abstract class TrackRContext<TEntityBase> where TEntityBase : class
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
        public void Add(TEntityBase entity)
        {
            var entitySet = GetEntitySet(entity);
            entitySet.AddEntity(entity);
        }

        /// <summary>
        /// Removes an entity from the context. Set will be determined automatically.
        /// </summary>
        /// <param name="entity"></param>
        public void Remove(TEntityBase entity)
        {
            var entitySet = GetEntitySet(entity);
            entitySet.RemoveEntity(entity);
        }

        /// <summary>
        /// Tracks an entity (attach).
        /// </summary>
        /// <param name="entity"></param>
        public void Track(TEntityBase entity)
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
                        if (v is TEntityBase)
                        {
                            Track(v as TEntityBase);
                        }
                    }
                }
                else
                {
                    var v = property.GetValue(entity);
                    if (v is TEntityBase)
                    {
                        Track(v as TEntityBase);
                    }
                }
            }
        }

        /// <summary>
        /// Tracks all entities in the collection.
        /// </summary>
        /// <param name="collection"></param>
        public void TrackMany(IEnumerable<TEntityBase> collection)
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
        public void UnTrack(TEntityBase entity)
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
                };
                var json = JsonConvert.SerializeObject(changetSet, settings);

                using (var client = CreateHttpClient())
                {
                    var result = await client.PostAsync(TrackRUri, new StringContent(json, Encoding.UTF8, "application/json"));

                    var content = await result.Content.ReadAsStringAsync();
                    content = content.Substring(1, content.Length - 2).Replace("\\\"", "\"");

                    if (!result.IsSuccessStatusCode)
                    {
                        throw new ServerException("Server returned: {0}\n{1}".FormatStatic(result.StatusCode, content));
                    }

                    var deserializeSettings = new JsonSerializerSettings
                    {
                        ContractResolver = new FlatJsonResolver(),
                        TypeNameHandling = TypeNameHandling.Objects,
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    };

                    var updatedChangeSet = JsonConvert.DeserializeObject<ChangeSet>(content, deserializeSettings);
                    foreach (var wrapper in updatedChangeSet.Entities)
                    {
                        if (wrapper.ChangeState != ChangeState.Deleted)
                        {
                            var tracker = EntitySets.SelectMany(s => s.EntitiesNonGeneric).First(t => t.Guid == wrapper.Guid);
                            Update(wrapper, tracker);
                            tracker.UpdateOriginal();
                        }
                    }
                    foreach (var tracker in EntitySets.SelectMany(s => s.EntitiesNonGeneric).ToList())
                    {
                        if (tracker.State == ChangeState.Deleted)
                        {
                            var set = GetEntitySet(tracker.GetEntity());
                            set.UnTrackEntity(tracker.GetEntity());
                        }
                        else
                        {
                            tracker.State = ChangeState.Unchanged;
                        }
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
                    Remove(tracker.GetEntity() as TEntityBase);
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
        private EntitySet GetEntitySet(object entity)
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
        protected abstract int GetId(object entity);

        /// <summary>
        /// Sets the id of an entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        protected abstract void SetId(object entity, int value);


        /// <summary>
        /// Updates an entity.
        /// </summary>
        /// <param name="wrapper"></param>
        /// <param name="tracker"></param>
        private void Update(EntityWrapper wrapper, EntityTracker tracker)
        {
            var oldEntity = tracker.GetEntity();
            var updatedEntity = wrapper.Entity;

            var properties = oldEntity.GetType().GetProperties();
            foreach (var property in properties)
            {
                if (property.CanRead &&
                    property.CanWrite &&
                    (property.PropertyType.IsValueType || property.PropertyType == typeof(string) || property.PropertyType.IsArray))
                {

                    property.SetValue(oldEntity, property.GetValue(updatedEntity));
                }
            }
        }


        /// <summary>
        /// For unit testing.
        /// </summary>
        /// <returns></returns>
        protected virtual HttpClient CreateHttpClient()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            return client;
        }
    }
}
