using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestData;
using TestData.Entities;

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
            catch
            {
                Debugger.Break();
            }
        }

        private class Runner
        {
            public async Task Run()
            {
                var context = new MyODataContext();
                var query = context.QueryContext.Patients
                    .Expand(p => p.Address)
                    .Expand("PatientInsurances");
                var result = await context.LoadManyAsync<Patient>(query);
                var patient = result.First();
                patient.FirstName = "Florian";


                await context.SubmitChangesAsync();
            }
        }
    }
}
