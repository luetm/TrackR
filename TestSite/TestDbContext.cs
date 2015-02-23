using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Web;
using TestData;

namespace TestSite
{
    public class TestDbContext : DbContext
    {
        public DbSet<Address> Addresses { get; set; }
        public DbSet<Associate> Associates { get; set; }
        public DbSet<Insurance> Insurances { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<PatientInsurance> PatientInsurances { get; set; }

        public TestDbContext() : base("TrackR_Test")
        {
            
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();
            modelBuilder.Conventions.Remove<ManyToManyCascadeDeleteConvention>();

            modelBuilder.Configurations.AddFromAssembly(GetType().Assembly);
        }
    }
}