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
using TrackR.Common.Interfaces;

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
        public List<EntitySet> EntitySets { get; }

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
        /// Handles different kinds of authentication methods.
        /// </summary>
        public IAuthBehavior AuthBehavior { get; set; }


        /// <summary>
        /// URI to the TrackR Controller.
        /// </summary>
        protected Uri TrackRUri;


        /// <summary>
        /// Base constructor.
        /// </summary>
        /// <param name="trackRUri"></param>
        protected TrackRContext(Uri trackRUri) : this()
        {
            TrackRUri = trackRUri;
            BaseUri = new UriBuilder(trackRUri) { Path = "", Query = "" }.Uri;
        }

        /// <summary>
        /// 
        /// </summary>
        protected TrackRContext()
        {
            EntitySets = new List<EntitySet>();
        }

        /// <summary>
        /// Initializes the context, in case the uri is not known at constructor time (IoC).
        /// </summary>
        /// <param name="trackRUri"></param>
        public void Initialize(Uri trackRUri)
        {
            TrackRUri = trackRUri;
            BaseUri = new UriBuilder(trackRUri) { Path = "", Query = "" }.Uri;
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

        public void AddDeep(TEntityBase entity)
        {
            var entitySet = GetEntitySet(entity);
            var id = GetId(entity);

            if (id != 0 && entitySet.EntitiesNonGeneric.Any(e => GetId(e.GetEntity()) == id))
                return;

            Add(entity);

            var properties = entity.GetType().GetProperties()
                .Where(p => !p.PropertyType.IsValueType && p.PropertyType != typeof(string) && p.GetValue(entity) != null)
                .ToList();

            foreach (var property in properties)
            {
                var value = property.GetValue(entity);
                if (value is IEnumerable)
                {
                    foreach (var v in value as IEnumerable)
                    {
                        if (v is TEntityBase)
                        {
                            AddDeep(v as TEntityBase);
                        }
                    }
                }
                else
                {
                    if (value is TEntityBase)
                    {
                        AddDeep(value as TEntityBase);
                    }
                }
            }
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
        /// Deeply removes an entity from the context.
        /// </summary>
        /// <param name="entity"></param>
        public void RemoveDeep(TEntityBase entity)
        {
            var entitySet = GetEntitySet(entity);
            var id = GetId(entity);

            if (entitySet.EntitiesNonGeneric.All(e => GetId(e.GetEntity()) != id))
                return;

            Remove(entity);

            var properties = entity.GetType().GetProperties()
                .Where(p => !p.PropertyType.IsValueType && p.PropertyType != typeof(string) && p.GetValue(entity) != null)
                .ToList();

            foreach (var property in properties)
            {
                var value = property.GetValue(entity);
                if (value is IEnumerable)
                {
                    foreach (var v in value as IEnumerable)
                    {
                        if (v is TEntityBase)
                        {
                            RemoveDeep(v as TEntityBase);
                        }
                    }
                }
                else
                {
                    if (value is TEntityBase)
                    {
                        RemoveDeep(value as TEntityBase);
                    }
                }
            }
        }

        /// <summary>
        /// Tracks an entity (attach).
        /// </summary>
        /// <param name="entity"></param>
        public void Track(TEntityBase entity)
        {
            var entitySet = GetEntitySet(entity);
            var id = GetId(entity);

            if (entitySet.EntitiesNonGeneric.Any(e => GetId(e.GetEntity()) == id))
                return;

            entitySet.TrackEntity(entity);

            var properties = entity.GetType().GetProperties()
                .Where(p => !p.PropertyType.IsValueType && p.PropertyType != typeof(string) && p.GetValue(entity) != null)
                .ToList();

            foreach (var property in properties)
            {
                var value = property.GetValue(entity);
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
                    if (value is TEntityBase)
                    {
                        Track(value as TEntityBase);
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
                // Get all pending changes & turn them into JSON
                var changetSet = BuildChangeSet();
                var settings = new JsonSerializerSettings
                {
                    ContractResolver = new FlatJsonResolver(),
                    TypeNameHandling = TypeNameHandling.Objects,
                    PreserveReferencesHandling = PreserveReferencesHandling.All,
                    MaxDepth = 100,
                };
                var json = JsonConvert.SerializeObject(changetSet, settings);

                // Send the changeset to the server
                using (var client = CreateHttpClient())
                {
                    if (AuthBehavior != null)
                    {
                        var authHeader = AuthBehavior.GetHeader();
                        client.DefaultRequestHeaders.Add(authHeader.Item1, authHeader.Item2);
                    }

                    var result = await client.PostAsync(TrackRUri, new StringContent(json, Encoding.UTF8, "application/json"));
                    var content = await result.Content.ReadAsStringAsync();

                    // Well.. I don't know why but sometimes the stuff comes out as an excaped string
                    // So lets fix it.
                    if (content.StartsWith("\""))
                    {
                        content = content.Substring(1, content.Length - 2).Replace("\\\"", "\"");
                    }

                    if (!result.IsSuccessStatusCode)
                    {
                        throw new ServerException("Server returned: {0}\n{1}".FormatStatic(result.StatusCode, content));
                    }

                    // Update entity trackers and sets
                    UpdateEntitySets(content);
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
                    UnTrack(tracker.GetEntity() as TEntityBase);
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

        public virtual void Clear<TEntity>() where TEntity : TEntityBase
        {
            Clear(typeof(TEntity));
        }

        public virtual void Clear(Type type)
        {
            var set = EntitySets.FirstOrDefault(x => x.Type == type.FullName);
            set?.Clear();
        }


        /// <summary>
        /// Compiles all changes from tracked entities.
        /// </summary>
        /// <returns></returns>
        protected ChangeSet BuildChangeSet()
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
        protected void BuildWrapper(EntityTracker entity, List<EntityWrapper> wrappers, List<EntityTracker> allEntities)
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
        protected EntitySet GetEntitySet(object entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

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
        /// 
        /// </summary>
        /// <param name="wrapper"></param>
        /// <param name="tracker"></param>
        protected void Update(EntityWrapper wrapper, EntityTracker tracker)
        {
            var oldEntity = tracker.GetEntity();
            var updatedEntity = wrapper.Entity;

            var properties = oldEntity.GetType().GetProperties();
            foreach (var property in properties)
            {
                if (property.CanRead &&
                    property.CanWrite &&
                    (property.PropertyType.IsValueType || property.PropertyType == typeof(string) || property.PropertyType.IsArray || property.PropertyType.Name.Contains("Nullable")))
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

        /// <summary>
        /// Updates the entity set with the updated values from the server.
        /// </summary>
        /// <param name="content"></param>
        protected void UpdateEntitySets(string content)
        {
            var deserializeSettings = new JsonSerializerSettings
            {
                ContractResolver = new FlatJsonResolver(),
                TypeNameHandling = TypeNameHandling.Objects,
                PreserveReferencesHandling = PreserveReferencesHandling.All,
                MaxDepth = 100,
                StringEscapeHandling = StringEscapeHandling.EscapeNonAscii,
            };

            content = content.Replace(@"\\n", @"\n").Replace(@"\\r", @"\r");

            var updatedChangeSet = JsonConvert.DeserializeObject<ChangeSet>(content, deserializeSettings);
            foreach (var wrapper in updatedChangeSet.Entities)
            {
                if (wrapper.ChangeState != ChangeState.Deleted)
                {
                    var tracker = EntitySets.SelectMany(s => s.EntitiesNonGeneric).First(t => t.Guid == wrapper.Guid);
                    tracker.Update(wrapper.Entity);
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
                    var isChanged = tracker.GetEntity().GetType().GetProperty("IsChanged");
                    isChanged?.SetValue(tracker.GetEntity(), false);
                }
            }
        }
    }
}
