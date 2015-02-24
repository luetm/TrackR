using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestData
{
    public class FixtureBase<TSut>
    {
        protected TSut Sut { get; set; }

        public TSut Build()
        {
            return Sut;
        }
    }
}
