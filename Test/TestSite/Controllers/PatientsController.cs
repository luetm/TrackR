using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using System.Web.Http.OData;
using TestData.Entities;

namespace TestSite.Controllers
{
    public class PatientsController : ODataController
    {
        private readonly TestDbContext _db = new TestDbContext();

        // GET: odata/Patients
        [EnableQuery]
        public IQueryable<Patient> GetPatients()
        {
            return _db.Patients;
        }

        // GET: odata/Patients(5)
        [EnableQuery]
        public SingleResult<Patient> GetPatient([FromODataUri] int key)
        {
            return SingleResult.Create(_db.Patients.Where(patient => patient.Id == key));
        }

        // PUT: odata/Patients(5)
        public IHttpActionResult Put([FromODataUri] int key, Delta<Patient> patch)
        {
            Validate(patch.GetEntity());

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Patient patient = _db.Patients.Find(key);
            if (patient == null)
            {
                return NotFound();
            }

            patch.Put(patient);

            try
            {
                _db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PatientExists(key))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Updated(patient);
        }

        // POST: odata/Patients
        public IHttpActionResult Post(Patient patient)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _db.Patients.Add(patient);
            _db.SaveChanges();

            return Created(patient);
        }

        // PATCH: odata/Patients(5)
        [AcceptVerbs("PATCH", "MERGE")]
        public IHttpActionResult Patch([FromODataUri] int key, Delta<Patient> patch)
        {
            Validate(patch.GetEntity());

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Patient patient = _db.Patients.Find(key);
            if (patient == null)
            {
                return NotFound();
            }

            patch.Patch(patient);

            try
            {
                _db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PatientExists(key))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Updated(patient);
        }

        // DELETE: odata/Patients(5)
        public IHttpActionResult Delete([FromODataUri] int key)
        {
            Patient patient = _db.Patients.Find(key);
            if (patient == null)
            {
                return NotFound();
            }

            _db.Patients.Remove(patient);
            _db.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
        }

        // GET: odata/Patients(5)/Address
        [EnableQuery]
        public SingleResult<Address> GetAddress([FromODataUri] int key)
        {
            return SingleResult.Create(_db.Patients.Where(m => m.Id == key).Select(m => m.Address));
        }

        // GET: odata/Patients(5)/Associate
        [EnableQuery]
        public SingleResult<Associate> GetAssociate([FromODataUri] int key)
        {
            return SingleResult.Create(_db.Patients.Where(m => m.Id == key).Select(m => m.Associate));
        }

        // GET: odata/Patients(5)/PatientInsurances
        [EnableQuery]
        public IQueryable<PatientInsurance> GetPatientInsurances([FromODataUri] int key)
        {
            return _db.Patients.Where(m => m.Id == key).SelectMany(m => m.PatientInsurances);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool PatientExists(int key)
        {
            return _db.Patients.Count(e => e.Id == key) > 0;
        }
    }
}
