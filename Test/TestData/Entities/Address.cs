namespace TestData.Entities
{
    public class Address : Entity
    {
        public string Street { get; set; }
        public string Zip { get; set; }
        public string City { get; set; }
        public string Country { get; set; }

        public Address()
        {
            Country = "CH";
        }
    }
}
