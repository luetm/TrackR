using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.Remoting;
using System.Text;
using TrackR.Common;

namespace TrackR.Client
{
    public abstract class TrackRContext
    {
        public List<EntitySet> EntitySets { get; private set; }

        protected readonly Uri TrackRUri;
        

        protected TrackRContext(Uri trackRUri)
        {
            TrackRUri = trackRUri;
            EntitySets = new List<EntitySet>();
        }


        public void Add(object entity)
        {
            var entitySet = GetEntitySet(entity);
            entitySet.AddEntity(entity);
        }

        public void Remove(object entity)
        {
            var entitySet = GetEntitySet(entity);
            entitySet.RemoveEntity(entity);
        }

        public void Track(object entity)
        {
            var entitySet = GetEntitySet(entity);
            entitySet.TrackEntity(entity);
        }

        public void UnTrack(object entity)
        {
            var entitySet = GetEntitySet(entity);
            entitySet.UnTrackEntity(entity);
        }

        public async void SubmitChanges()
        {
            var changetSet = BuildChangeSet();

            var settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            };
            var json = JsonConvert.SerializeObject(changetSet, settings);

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("Content-Type", "application/json");
                var result = await client.PostAsync(TrackRUri, new StringContent(json, Encoding.UTF8));
                var content = await result.Content.ReadAsStringAsync();

                if (!result.IsSuccessStatusCode)
                {
                    throw new ServerException("Server returned: {0}\n{2}".F(result.StatusCode, content));
                }
            }
        }


        private ChangeSet BuildChangeSet()
        {
            var entities = EntitySets.SelectMany(s => s.EntitiesNonGeneric.Cast<EntityTracker>()).ToList();
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
        
        private EntitySet GetEntitySet(object entity)
        {
            if (entity == null)
                throw new ArgumentNullException("entity");

            var entitySet = EntitySets.SingleOrDefault(s => s.Type == entity.GetType().FullName);
            if (entitySet == null)
            {
                var newSetType = typeof(EntitySet<>).MakeGenericType(entity.GetType());
                var newSet = (EntitySet)Activator.CreateInstance(newSetType);
                return newSet;
            }

            return entitySet;
        }


        private List<object> ToAddSet(IEnumerable<EntityTracker> source)
        {
            return source.Select(s => s.GetEntity()).ToList();
        }

        private List<int> ToRemoveSet(IEnumerable<EntityTracker> source)
        {
            return source.Select(s => GetId(s.GetEntity())).ToList();
        }

        private List<JsonPropertySet> ToEditSet(IEnumerable<EntityTracker> source)
        {
            return source.Select(entity => new JsonPropertySet
            {
                Id = GetId(entity),
                EntityType = entity.GetType().FullName,
                ChangedProperties = GetDelta(entity.GetEntity(), entity.GetOriginal())
            }).ToList();
        }


        private List<JsonTuple> GetDelta(object entity, object original)
        {
            var properties = entity.GetType().GetProperties()
                .Where(t => t.CanRead && t.CanWrite)
                .Where(t => t.PropertyType.IsValueType || t.PropertyType == typeof(string))
                .Where(t => t.GetCustomAttributes(true).Any(a => a is JsonIgnoreAttribute))
                .ToList();

            return properties
                .Where(p => p.GetValue(entity).Equals(p.GetValue(original)))
                .Select(p => new JsonTuple
                {
                    PropertyName = p.Name,
                    PropertyType = p.GetType().FullName,
                    JsonValue = JsonConvert.SerializeObject(p.GetValue(entity)),
                })
                .ToList();
        }

        protected abstract int GetId(object entity);
    }
}
