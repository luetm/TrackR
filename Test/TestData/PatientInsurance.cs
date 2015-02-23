using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestData
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
