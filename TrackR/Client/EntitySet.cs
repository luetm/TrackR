using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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

        public abstract IList EntitiesNonGeneric
        {
            get;
        }

        public abstract void AddEntity(object entity);

        public abstract void RemoveEntity(object entity);

        public abstract void TrackEntity(object entity);

        public abstract void UnTrackEntity(object entity);
    }

    /// <summary>
    /// Collets entities of a type.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public class EntitySet<TEntity> : EntitySet
    {
        public List<EntityTracker<TEntity>> Entities { get; set; }

        public override IList EntitiesNonGeneric
        {
            get { return Entities; }
        }


        public override void AddEntity(object entity)
        {
            var e = (TEntity)entity;
            var tracker = Entities.First(t => t.Entity.Equals(e));
            if (tracker != null)
                throw new InvalidOperationException("Cannot add entity to set: Entity was already added.");

            tracker = new EntityTracker<TEntity>(e)
            {
                State = ChangeState.Added
            };
            Entities.Add(tracker);
        }

        public override void RemoveEntity(object entity)
        {
            try
            {
                var e = (TEntity)entity;
                var tracker = Entities.First(t => t.Entity.Equals(e));
                tracker.State = ChangeState.Deleted;
            }
            catch (InvalidOperationException)
            {
                throw new InvalidOperationException("Cannot remove entity from set: Entity not found.");
            }
        }

        public override void TrackEntity(object entity)
        {
            var e = (TEntity)entity;
            var tracker = Entities.First(t => t.Entity.Equals(e));
            if (tracker != null) return;

            tracker = new EntityTracker<TEntity>(e)
            {
                State = ChangeState.Unchanged
            };
            Entities.Add(tracker);
        }

        public override void UnTrackEntity(object entity)
        {
            var e = (TEntity)entity;
            var tracker = Entities.First(t => t.Entity.Equals(e));
            if (tracker == null) return;

            Entities.Remove(tracker);
        }


        public void Add(TEntity entity)
        {
            AddEntity(entity);
        }

        public void Remove(TEntity entity)
        {
            RemoveEntity(entity);
        }

        public void Track(TEntity entity)
        {
            TrackEntity(entity);
        }

        public void UnTrack(TEntity entity)
        {
            UnTrackEntity(entity);
        }
    }
}
