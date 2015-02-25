using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using TestData.Builders;
using TestData.Entities;
using TrackR.Common;

namespace UnitTests.Common
{
    [TestFixture]
    public class ExtensionTests
    {
        [Test]
        public void FormatStatic_Called_Works()
        {
            // * Arrange

            // * Act
            var result = "{0}-{1}".FormatStatic("foo", "bar");

            // * Assert
            Assert.AreEqual(string.Format("{0}-{1}", "foo", "bar"), result);
        }

        [Test]
        public void IsNullOrWhitespace_Called_Works()
        {
            // * Arrange

            // * Act
            var r1 = ((string)null).IsNullOrWhiteSpace();
            var r2 = "".IsNullOrWhiteSpace();
            var r4 = " ".IsNullOrWhiteSpace();
            var r3 = "\t".IsNullOrWhiteSpace();
            var r5 = "\n".IsNullOrWhiteSpace();
            var r6 = "\n\t ".IsNullOrWhiteSpace();
            var r7 = "foo".IsNullOrWhiteSpace();
            var r8 = " foo\t".IsNullOrWhiteSpace();

            // * Assert
            Assert.IsTrue(new[] { r1, r2, r3, r4, r5, r6 }.All(b => b));
            Assert.IsTrue(new[] { r7, r8 }.All(b => b == false));
        }

        [Test]
        public void ToUriParameter_Called_Works()
        {
            // * Arrange
            
            // * Act
            var r1 = new DateTime(2000, 1, 1).ToUriParameter("date");
            var r2 = 5.ToUriParameter("id");
            var r3 = "bar".ToUriParameter("foo");
            var r31 = "bar bar".ToUriParameter("foo");
            var r4 = ((string)null).ToUriParameter("optional");
            
            // * Assert
            Assert.AreEqual("date=2000-01-01", r1);
            Assert.AreEqual("id=5", r2);
            Assert.AreEqual("foo=bar", r3);
            Assert.AreEqual("foo=bar%20bar", r31);
            Assert.AreEqual("optional=NULL", r4);
        }

        [Test]
        public void DeepInject_Called_Works()
        {
            // * Arrange
            var pSrc = new PatientBuilder().Get();

            // * Act
            var pDest = pSrc.DeepInject<Patient>();

            // * Assert
            Assert.IsNotNull(pDest.Address);
            Assert.AreNotEqual(pSrc.Address, pDest.Address);
            Assert.AreEqual(null, pDest.Associate);
            Assert.AreEqual(null, pDest.PatientInsurances);

            Assert.AreEqual(pSrc.AddressId, pDest.AddressId);
            Assert.AreEqual(pSrc.AssociateId, pDest.AssociateId);
            Assert.AreEqual(pSrc.FirstName, pDest.FirstName);
            Assert.AreEqual(pSrc.LastName, pDest.LastName);
        }

        [Test]
        public void FirstQuery_Called_Works()
        {
            // * Arrange
            var patients = new PatientBuilder().Get(10).ToList();

            // * Act
            var result = patients.AsQueryable().FirstQuery().ToList();
            
            // * Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(patients.First(), result.Single());
        }
    }
}
