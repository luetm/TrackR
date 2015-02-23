using System;
using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Web;
using TestData;
using TestData.Entities;

namespace TestSite.Models
{
    public class PatientConfig : EntityTypeConfiguration<Patient>
    {
        public PatientConfig()
        {
            HasMany(p => p.PatientInsurances).WithRequired(pi => pi.Patient).WillCascadeOnDelete();
        }
    }
}