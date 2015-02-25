using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestData.Entities;

namespace TestData.Builders
{
    public class PatientBuilder : BuilderBase<Patient, PatientBuilder>
    {
        public PatientBuilder()
        {
            Entity = new Patient();
            With(p => p.FirstName = "John");
            With(p => p.LastName = "Doe");
            WithAddress(new AddressBuilder().Get());
        }

        public PatientBuilder WithAddress(Address address)
        {
            return With(p => p.Address = address);
        }

        public PatientBuilder WithAssociate(string name = "Johann Doch", string role = "Father", Address address = null)
        {
            var addr = address ?? new AddressBuilder().Get();
            return With(a => a.Associate = new Associate
            {
                Name = name,
                Role = role,
                Address = addr,
            });
        }

        public PatientBuilder WithPatientInsurance(string insuranceNumber, Insurance insurance = null)
        {
            if (Entity.PatientInsurances == null)
            {
                Entity.PatientInsurances = new List<PatientInsurance>();
            }

            insurance = insurance ?? new InsuranceBuilder().Get();

            Entity.PatientInsurances.Add(new PatientInsurance
            {
                Insurance = insurance,
                InsuranceId = insurance.Id,
                InsuranceNumber = insuranceNumber,
                Patient = Entity,
            });

            return this;
        }

        public PatientBuilder Complete()
        {
            return WithAssociate("Jason Doe", "Father", new AddressBuilder().Get())
                .WithPatientInsurance("123.456");
        }

        public PatientBuilder WithoutAddress()
        {
            Entity.Address = null;
            return this;
        }
    }
}
