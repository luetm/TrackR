using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using TrackR.Common;

namespace TestSite.Controllers
{
    public class TrackRController : ApiController
    {
        private readonly TestDbContext _context;

        public TrackRController()
        {
            _context = new TestDbContext();
        }

        public IHttpActionResult Post(ChangeSet changeSet)
        {
            if (changeSet == null)
            {
                return BadRequest();
            }

            foreach (var add in changeSet.ToAdd)
            {
                ApplyState(add, EntityState.Detached);
            }

            foreach (var edit in changeSet.ToEdit)
            {
                ApplyState(edit, EntityState.Modified);
            }

            foreach (var remove in changeSet.ToDelete)
            {
                ApplyState(remove, EntityState.Deleted);
            }
            _context.SaveChanges();

            return Ok();
        }

        private void ApplyState(object o, EntityState idNonZeroState, List<object> processed = null)
        {
            if (processed == null)
            {
                processed = new List<object>();
            }

            if (processed.Contains(o)) 
                return;

            processed.Add(o);

            if (o == null) return;

            var idProp = o.GetType().GetProperty("Id");
            if (idProp == null) return;

            if ((int) idProp.GetValue(o) == 0)
            {
                _context.Entry(o).State = EntityState.Added;
            }
            else
            {
                _context.Entry(o).State = idNonZeroState;
            }

            foreach (var prop in o.GetType().GetProperties())
            {
                if (!prop.PropertyType.IsValueType && prop.PropertyType != typeof (string))
                {
                    var value = prop.GetValue(o);
                    if (value == null) continue;

                    if (value is IEnumerable)
                    {
                        foreach (var v in value as IEnumerable)
                        {
                            ApplyState(v, idNonZeroState, processed);
                        }
                    }
                    else
                    {
                        ApplyState(value, idNonZeroState, processed);
                    }
                }
            }
        }
    }
}
