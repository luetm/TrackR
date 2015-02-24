using System;
using System.Collections.Generic;

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
            return Entity;
        }

        public IEnumerable<TEntity> Get(int amount)
        {
            var result = new List<TEntity>();
            for (int i = 1; i <= amount; i++)
            {
                var copyMethod = new Func<TEntity, TEntity>(ObjectExtensions.Copy);
                var e = copyMethod.Invoke(Entity);
                e.GetType().GetProperty("Id").SetValue(e, i);
                result.Add(e);
            }

            return result;
        }
    }
}
