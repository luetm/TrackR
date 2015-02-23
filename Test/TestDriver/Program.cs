using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestData;

namespace TestDriver
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var runner = new Runner();
                runner.Run().Wait();
            }
            catch (Exception err)
            {
                Debugger.Break();
            }
        }

        private class Runner
        {
            public async Task Run()
            {
                var context = new MyODataContext();

                var patient = new Patient
                {
                    Id = 2,
                    LastName = "Schwärzl",
                    FirstName = "Thomas",
                    AddressId = 1,
                    Address = new Address
                    {
                        Id = 1,
                        Street = "Unterdorfstrasse 14",
                        Zip = "9100",
                        City = "Herisau",
                    },
                    PatientInsurances = new List<PatientInsurance>
                {
                    new PatientInsurance
                    {
                        Id = 1,
                        PatientId = 1,
                        InsuranceId = 1,
                        InsuranceNumber = "123.456",
                    }
                },
                    AssociateId = 1,
                    Associate = new Associate
                    {
                        Id = 1,
                        Name = "Lukas Langenegger",
                        Role = "Mentor",
                        AddressId = 2,
                        Address = new Address
                        {
                            Id = 2,
                            Street = "Oberdorfstrasse 66",
                            Zip = "9100",
                            City = "Herisau",
                        },
                    },
                };

                //context.Track(patient);
                patient.FirstName = "Odin";

                //var addr = new Address
                //{
                //    Street = "Achstrasse 1",
                //    City = "Herisau",
                //    Zip = "9100"
                //};
                //context.Add(addr);

                context.Track(patient);
                context.Remove(patient);
                
                await context.SubmitChangesAsync();
            }
        }
    }
}
