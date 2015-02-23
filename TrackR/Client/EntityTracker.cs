using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Omu.ValueInjecter;

namespace TrackR.Client
{
    /// <summary>
    /// Wrapper aroudn an entity that tracks its changes.
    /// </summary>
    public abstract class EntityTracker
    {
        /// <summary>
        /// Current state of the entity.
        /// </summary>
        public ChangeState State { get; internal set; }

        /// <summary>
        /// Gets the entity object. For the generic variant use the generic variant of this class.
        /// </summary>
        /// <returns></returns>
        public abstract object GetEntity();

        /// <summary>
        /// Gets the original entity object (unchanged copy). For the generic variant use the generic variant of this class.
        /// </summary>
        /// <returns></returns>
        public abstract object GetOriginal();

        /// <summary>
        /// Reverts the entity to the original.
        /// </summary>
        public abstract void RevertToOriginal();
    }

    /// <summary>
    /// Wrapper aroudn an entity that tracks its changes.
    /// </summary>
    public class EntityTracker<TEntity> : EntityTracker
    {
        /// <summary>
        /// Gets the entity object.
        /// </summary>
        public TEntity Entity { get; private set; }

        /// <summary>
        /// The original entity object (unchanged copy).
        /// </summary>
        public TEntity Original { get; private set; }


        /// <summary>
        /// Creates a new entity tracker.
        /// </summary>
        /// <param name="entity"></param>
        public EntityTracker(TEntity entity)
        {
            Entity = entity;
            Original = (TEntity)Activator.CreateInstance(typeof(TEntity)).InjectFrom(entity);
        }

        /// <summary>
        /// Gets the entity object. For the generic variant use the generic variant of this class.
        /// </summary>
        /// <returns></returns>
        public override object GetEntity()
        {
            return Entity;
        }

        /// <summary>
        ///  Gets the original entity object (unchanged copy). For the generic variant use the generic variant of this class.
        /// </summary>
        /// <returns></returns>
        public override object GetOriginal()
        {
            return Original;
        }

        /// <summary>
        /// Reverts the entity to the original.
        /// </summary>
        public override void RevertToOriginal()
        {
            Entity.InjectFrom(Original);
        }
    }
}
