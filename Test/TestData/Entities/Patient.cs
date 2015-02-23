using System.Collections.Generic;

namespace TestData.Entities
{
    public class Patient : Entity
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public virtual Address Address { get; set; }
        public int AddressId { get; set; }

        public virtual Associate Associate { get; set; }
        public int AssociateId { get; set; }

        public virtual ICollection<PatientInsurance> PatientInsurances { get; set; }
    }
}
