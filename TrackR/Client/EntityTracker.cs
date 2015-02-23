using Omu.ValueInjecter;
using System;
using System.ComponentModel;

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
        public abstract INotifyPropertyChanged GetEntity();

        /// <summary>
        /// Gets the original entity object (unchanged copy). For the generic variant use the generic variant of this class.
        /// </summary>
        /// <returns></returns>
        public abstract INotifyPropertyChanged GetOriginal();

        /// <summary>
        /// Reverts the entity to the original.
        /// </summary>
        public abstract void RevertToOriginal();
    }

    /// <summary>
    /// Wrapper aroudn an entity that tracks its changes.
    /// </summary>
    public class EntityTracker<TEntity> : EntityTracker where TEntity : INotifyPropertyChanged
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
            Entity.PropertyChanged += OnEntityPropertyChanged;

            Original = (TEntity)Activator.CreateInstance(typeof(TEntity)).InjectFrom(entity);
        }

        /// <summary>
        /// Used to set modified flag.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnEntityPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            State = ChangeState.Changed;
        }

        /// <summary>
        /// Gets the entity object. For the generic variant use the generic variant of this class.
        /// </summary>
        /// <returns></returns>
        public override INotifyPropertyChanged GetEntity()
        {
            return Entity;
        }

        /// <summary>
        ///  Gets the original entity object (unchanged copy). For the generic variant use the generic variant of this class.
        /// </summary>
        /// <returns></returns>
        public override INotifyPropertyChanged GetOriginal()
        {
            return Original;
        }

        /// <summary>
        /// Reverts the entity to the original.
        /// </summary>
        public override void RevertToOriginal()
        {
            Entity.InjectFrom(Original);
            State = ChangeState.Unchanged;
        }
    }
}
