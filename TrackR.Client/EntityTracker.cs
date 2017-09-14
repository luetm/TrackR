using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Omu.ValueInjecter;
using TrackR.Common;

namespace TrackR.Client
{
    /// <summary>
    /// Wrapper aroudn an entity that tracks its changes.
    /// </summary>
    public abstract class EntityTracker
    {
        /// <summary>
        /// Guid that identifies this entity for the tracker.
        /// </summary>
        public Guid Guid { get; set; }

        /// <summary>
        /// Current state of the entity.
        /// </summary>
        public ChangeState State { get; set; }

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

        /// <summary>
        /// Updates the original with the old value.
        /// </summary>
        internal abstract void UpdateOriginal();

        /// <summary>
        /// Updates the original and the entity with newer values.
        /// </summary>
        /// <param name="newer"></param>
        public abstract void Update(object newer);
    }

    /// <summary>
    /// Wrapper aroudn an entity that tracks its changes.
    /// </summary>
    public class EntityTracker<TEntity> : EntityTracker
    {
        /// <summary>
        /// Gets the entity object.
        /// </summary>
        public TEntity Entity { get; }

        /// <summary>
        /// The original entity object (unchanged copy).
        /// </summary>
        public TEntity Original { get; private set; }


        /// <summary>
        /// Creates a new entity tracker.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="added"></param>
        public EntityTracker(TEntity entity, bool added = false)
        {
            Entity = entity;

            if (Entity is INotifyPropertyChanged)
            {
                var inpc = (INotifyPropertyChanged)Entity;
                inpc.PropertyChanged += OnEntityPropertyChanged;
            }
            else
            {
                throw new NotSupportedException("Only INotifyPropertyChanged entities are supported so far.");
            }
            Guid = Guid.NewGuid();

            if (added)
            {
                State = ChangeState.Added;
            }

            Original = (TEntity)Activator.CreateInstance(typeof(TEntity)).InjectFrom(entity);
        }

        /// <summary>
        /// Used to set modified flag.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnEntityPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var property = Entity.GetType().GetProperty(e.PropertyName);
            if (property?.GetCustomAttributes().OfType<DontTrackAttribute>().Any() ?? true)
                return;

            if (State == ChangeState.Unchanged)
            {
                try
                {
                    var eType = Entity?.GetType();
                    var eProp = eType.GetProperty(e.PropertyName);
                    var eVal = eProp?.GetValue(Entity);

                    var oType = Original?.GetType();
                    var oProp = oType?.GetProperty(e.PropertyName);
                    var oVal = oProp?.GetValue(Original);

                    // Entity Value == Original Value => no change!!!
                    if ((eVal == null && oVal == null) ||
                        (eVal != null && eVal.Equals(oVal)))
                        return;
                }
                catch { /* Not now */ }

                State = ChangeState.Changed;
            }
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
            State = ChangeState.Unchanged;
        }

        /// <summary>
        /// Updates the original to the current entity.
        /// </summary>
        internal override void UpdateOriginal()
        {
            Original = (TEntity)Activator.CreateInstance(typeof(TEntity)).InjectFrom(Entity);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newer"></param>
        public override void Update(object newer)
        {
            var properties = Entity.GetType().GetProperties();
            foreach (var property in properties)
            {
                if (property.CanRead &&
                    property.CanWrite &&
                    (property.PropertyType.IsValueType ||
                    property.PropertyType == typeof(string) ||
                    property.PropertyType.IsArray ||
                    property.PropertyType.Name.Contains("Nullable")))
                {
                    property.SetValue(Entity, property.GetValue(newer));
                }
            }
        }
    }
}
