namespace TestData.Entities
{
    public class PatientInsurance : Entity
    {
        public string InsuranceNumber { get; set; }

        public virtual Patient Patient { get; set; }
        public int PatientId { get; set; }

        public virtual Insurance Insurance { get; set; }
        public int InsuranceId { get; set; }
    }
}
