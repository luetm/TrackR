
using TestData.Entities;

namespace TestData.Builders
{
    public class AddressBuilder : BuilderBase<Address, AddressBuilder>
    {
        public AddressBuilder()
        {
            Entity = new Address();
            With(a => a.Street = "Musterstrasse 100a")
                .With(a => a.Zip = "9100")
                .With(a => a.City = "Herisau")
                .With(a => a.Country = "CH");
        }
    }
}
