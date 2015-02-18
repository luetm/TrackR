using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Omu.ValueInjecter;

namespace TrackR.Client
{

    public abstract class EntityTracker
    {
        public ChangeState State { get; internal set; }

        public abstract object GetEntity();

        public abstract object GetOriginal();
    }

    public class EntityTracker<TEntity> : EntityTracker
    {
        public TEntity Entity { get; private set; }
        public TEntity Original { get; private set; }

        public EntityTracker(TEntity entity)
        {
            Entity = entity;
            Original = (TEntity)Activator.CreateInstance(typeof(TEntity)).InjectFrom(entity);
        }

        public override object GetEntity()
        {
            return Entity;
        }

        public override object GetOriginal()
        {
            return Original;
        }
    }
}
