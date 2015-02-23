namespace TestData.Entities
{
    public class Associate : Entity
    {
        public string Name { get; set; }
        public string Role { get; set; }

        public virtual Address Address { get; set; }
        public int AddressId { get; set; }
    }
}
