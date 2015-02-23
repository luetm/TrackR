using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestData
{
    public class Associate : Entity
    {
        public string Name { get; set; }
        public string Role { get; set; }

        public virtual Address Address { get; set; }
        public int AddressId { get; set; }
    }
}
