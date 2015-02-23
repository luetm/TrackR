using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Remoting;
using System.Text;
using System.Threading.Tasks;
using TrackR.Common;

namespace TrackR.Client
{
    /// <summary>
    /// Base class of specific (webapi, odata, ...) TrackR contexts.
    /// </summary>
    public abstract class TrackRContext
    {
        /// <summary>
        /// Entity sets contained in this context.
        /// </summary>
        public List<EntitySet> EntitySets { get; private set; }

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
                    TypeNameHandling = TypeNameHandling.Objects,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
#if DEBUG
                    Formatting = Formatting.Indented,
#endif
                };
                var json = JsonConvert.SerializeObject(changetSet, settings);

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    var result = await client.PostAsync(TrackRUri, new StringContent(json, Encoding.UTF8, "application/json"));
                    var content = await result.Content.ReadAsStringAsync();

                    if (!result.IsSuccessStatusCode)
                    {
                        throw new ServerException("Server returned: {0}\n{1}".F(result.StatusCode, content));
                    }
                }
            }
            catch (Exception e)
            {
                throw new WebException("Could not save changes. See inner exception for details.", e);
            }
        }


        /// <summary>
        /// Compiles all changes from tracked entities.
        /// </summary>
        /// <returns></returns>
        private ChangeSet BuildChangeSet()
        {
            var entities = EntitySets.SelectMany(s => s.EntitiesNonGeneric).ToList();
            var toAdd = entities.Where(e => e.State == ChangeState.Added).ToList();
            var toChange = entities.Where(e => e.State == ChangeState.Changed).ToList();
            var toDelete = entities.Where(e => e.State == ChangeState.Deleted).ToList();

            return new ChangeSet
            {
                ToAdd = ToAddSet(toAdd),
                ToEdit = ToEditSet(toChange),
                ToDelete = ToRemoveSet(toDelete),
            };
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
        /// Compiles a list of entities to add.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private List<INotifyPropertyChanged> ToAddSet(IEnumerable<EntityTracker> source)
        {
            return source.Select(s => s.GetEntity()).ToList();
        }

        /// <summary>
        /// Compiles a list of entities to remove.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private List<JsonIdReference> ToRemoveSet(IEnumerable<EntityTracker> source)
        {
            return source.Select(s => new JsonIdReference
            {
                Id = GetId(s.GetEntity()),
                Type = s.GetEntity().GetType().FullName,
            }).ToList();
        }

        /// <summary>
        /// Compiles a list of entities to edit.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private List<JsonPropertySet> ToEditSet(IEnumerable<EntityTracker> source)
        {
            return source.Select(entity => new JsonPropertySet
            {
                Id = GetId(entity.GetEntity()),
                EntityType = entity.GetEntity().GetType().FullName,
                ChangedProperties = GetDelta(entity.GetEntity(), entity.GetOriginal())
            }).ToList();
        }

        /// <summary>
        /// Gets the delta of two objects.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="original"></param>
        /// <returns></returns>
        private List<JsonTuple> GetDelta(INotifyPropertyChanged entity, INotifyPropertyChanged original)
        {
            var properties = entity.GetType().GetProperties()
                .Where(t => t.CanRead && t.CanWrite)
                .Where(t => t.PropertyType.IsValueType || t.PropertyType == typeof(string))
                .Where(t => !t.GetCustomAttributes(true).Any(a => a is JsonIgnoreAttribute))
                .ToList();

            var result = properties
                .Where(p => !p.GetValue(entity).Equals(p.GetValue(original)))
                .Select(p => new JsonTuple
                {
                    PropertyName = p.Name,
                    PropertyType = p.PropertyType.FullName,
                    JsonValue = JsonConvert.SerializeObject(p.GetValue(entity)),
                })
                .ToList();

            return result;
        }
        
        /// <summary>
        /// Gets an ID of an entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected abstract int GetId(INotifyPropertyChanged entity);
    }
}
