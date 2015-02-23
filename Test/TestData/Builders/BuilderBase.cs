using System;
using System.Collections.Generic;

namespace TestData.Builders
{
    public abstract class BuilderBase<TEntity, TBuilder> 
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
                var e = Entity.GetType().GetMethod("Copy").Invoke(Entity, null);
                e.GetType().GetProperty("Id").SetValue(e, i);
                result.Add((TEntity)e);
            }

            return result;
        }
    }
}
