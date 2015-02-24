using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using TrackR.Common;

namespace TrackR.Client
{
    /// <summary>
    /// Non-Generic version of the EntitySet class.
    /// </summary>
    public abstract class EntitySet
    {
        /// <summary>
        /// Full type name of the entity in the set.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Returns the list of entities in this entity set. For the generic variant use the generic variant of this class.
        /// </summary>
        public abstract IEnumerable<EntityTracker> EntitiesNonGeneric { get; }

        /// <summary>
        /// Adds an entity to the collection. For the generic variant use the generic variant of this class.
        /// </summary>
        /// <param name="entity"></param>
        public abstract void AddEntity(object entity);

        /// <summary>
        /// Removes an entity from the collection. For the generic variant use the generic variant of this class.
        /// </summary>
        /// <param name="entity"></param>
        public abstract void RemoveEntity(object entity);

        /// <summary>
        /// Start tracking an (attach). For the generic variant use the generic variant of this class.
        /// </summary>
        /// <param name="entity"></param>
        public abstract void TrackEntity(object entity);

        /// <summary>
        /// Untracks an entity (detach). For the generic variant use the generic variant of this class.
        /// </summary>
        /// <param name="entity"></param>
        public abstract void UnTrackEntity(object entity);


        /// <summary>
        /// Removes all entities from the set.
        /// </summary>
        public abstract void Clear();
    }

    /// <summary>
    /// Collets entities of a type.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public class EntitySet<TEntity> : EntitySet where TEntity : INotifyPropertyChanged
    {
        /// <summary>
        /// The entities in this set.
        /// </summary>
        public List<EntityTracker<TEntity>> Entities { get; set; }

        /// <summary>
        /// Returns the list of entities in this entity set.
        /// </summary>
        public override IEnumerable<EntityTracker> EntitiesNonGeneric
        {
            get { return Entities; }
        }


        /// <summary>
        /// Ctor.
        /// </summary>
        public EntitySet()
        {
            Entities = new List<EntityTracker<TEntity>>();
            Type = typeof(TEntity).FullName;
        }


        /// <summary>
        /// Adds an entity to the collection. Non generic variant.
        /// </summary>
        /// <param name="entity"></param>
        public override void AddEntity(object entity)
        {
            var e = (TEntity)entity;
            var tracker = Entities.FirstOrDefault(t => t.Entity.Equals(e));
            if (tracker != null)
                throw new InvalidOperationException("Cannot add entity to set: Entity was already added.");

            tracker = new EntityTracker<TEntity>(e)
            {
                State = ChangeState.Added
            };
            Entities.Add(tracker);
        }

        /// <summary>
        /// Removes an entity from the collection. Non generic variant.
        /// </summary>
        /// <param name="entity"></param>
        public override void RemoveEntity(object entity)
        {
            try
            {
                var e = (TEntity)entity;
                var tracker = Entities.First(t => t.Entity.Equals(e));
                if (tracker.State == ChangeState.Added)
                {
                    UnTrackEntity(entity);
                }
                else
                {
                    tracker.State = ChangeState.Deleted;
                }
            }
            catch (InvalidOperationException)
            {
                throw new InvalidOperationException("Cannot remove entity from set: Entity not found.");
            }
        }

        /// <summary>
        /// Start tracking an (attach). Non generic variant.
        /// </summary>
        /// <param name="entity"></param>
        public override void TrackEntity(object entity)
        {
            var e = (TEntity)entity;
            var tracker = Entities.FirstOrDefault(t => t.Entity.Equals(e));
            if (tracker != null) return;

            tracker = new EntityTracker<TEntity>(e)
            {
                State = ChangeState.Unchanged,
            };
            Entities.Add(tracker);
        }

        /// <summary>
        /// Untracks an entity (detach). Non generic variant.
        /// </summary>
        /// <param name="entity"></param>
        public override void UnTrackEntity(object entity)
        {
            var e = (TEntity)entity;
            var tracker = Entities.FirstOrDefault(t => t.Entity.Equals(e));
            if (tracker == null) return;

            Entities.Remove(tracker);
        }

        /// <summary>
        /// Clears all entities from the set.
        /// </summary>
        public override void Clear()
        {
            Entities.Clear();
        }


        /// <summary>
        /// Adds an entity to the collection.
        /// </summary>
        /// <param name="entity"></param>
        public void Add(TEntity entity)
        {
            AddEntity(entity);
        }

        /// <summary>
        /// Removes an entity from the collection.
        /// </summary>
        /// <param name="entity"></param>
        public void Remove(TEntity entity)
        {
            RemoveEntity(entity);
        }

        /// <summary>
        /// Start tracking an (attach).
        /// </summary>
        /// <param name="entity"></param>
        public void Track(TEntity entity)
        {
            TrackEntity(entity);
        }

        /// <summary>
        /// Untracks an entity (detach).
        /// </summary>
        /// <param name="entity"></param>
        public void UnTrack(TEntity entity)
        {
            UnTrackEntity(entity);
        }
    }
}
