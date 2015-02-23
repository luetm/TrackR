using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestData.Entities;

namespace TestData.Builders
{
    public class InsuranceBuilder : BuilderBase<Insurance, InsuranceBuilder>
    {
        public InsuranceBuilder()
        {
            Entity = new Insurance();
            With(i => i.Id = 1)
                .With(i => i.Name = "Morbidus Versicherung")
                .With(i => i.Type = "KVG");
        }
    }
}
