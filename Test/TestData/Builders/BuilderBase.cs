using System;
using System.Collections.Generic;
using Omu.ValueInjecter;

namespace TestData.Builders
{
    public abstract class BuilderBase<TEntity, TBuilder>  where TEntity : new()
        where TBuilder : class
    {
        protected TEntity Entity { get; set; }

        public TBuilder With(Action<TEntity> expression)
        {
            expression.Invoke(Entity);
            return this as TBuilder;
        }

        public TEntity Get()
        {
            return (TEntity)new TEntity().InjectFrom(Entity);
        }

        public virtual IEnumerable<TEntity> Get(int amount)
        {
            var result = new List<TEntity>();
            for (int i = 1; i <= amount; i++)
            {
                var e = Get();
                e.GetType().GetProperty("Id").SetValue(e, i);
                result.Add(e);
            }

            return result;
        }
    }
}
