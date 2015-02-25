using NUnit.Framework;
using System;
using TestData;
using TestData.Builders;
using TestData.Entities;
using TrackR.Client;
using TrackR.Common;

namespace UnitTests.Client
{
    public class EntityTrackerTests
    {
        [Test]
        public void Ctor_Called_PropertiesSetUp()
        {
            // * Arrange
            var fixture = new Fixture();
            
            // * Act
            var sut = fixture.Build();

            // * Assert
            Assert.AreNotEqual(sut.Guid, default(Guid));
            Assert.AreEqual(sut.State, ChangeState.Unchanged);
            Assert.IsNotNull(sut.Entity);
            Assert.IsNotNull(sut.Original);
            Assert.IsNotNull(sut.GetEntity());
            Assert.IsNotNull(sut.GetOriginal());
        }

        [Test]
        public void EntityChanged_Done_StateChanged()
        {
            // * Arrange
            var fixture = new Fixture();
            var sut = fixture.Build();

            // * Act
            sut.Entity.FirstName = "Ueli";

            // * Assert
            Assert.AreEqual(sut.State, ChangeState.Changed);
        }

        [Test]
        public void RevertToOriginal_Called_Reverted()
        {
            // * Arrange
            var patient = new PatientBuilder().With(p => p.FirstName = "Kari").Get();
            var fixture = new Fixture(patient);
            var sut = fixture.Build();

            // * Act
            sut.Entity.FirstName = "Ueli";
            sut.RevertToOriginal();

            // * Assert
            Assert.AreEqual(sut.Entity.FirstName, "Kari");
            Assert.AreEqual(sut.State, ChangeState.Unchanged);
        }

        [Test]
        public void EntityChanged_Done_OriginalStaysTheSame()
        {
            // * Arrange
            var fixture = new Fixture();
            var sut = fixture.Build();

            // * Act
            sut.Entity.FirstName = "Ueli";
            
            // * Assert
            Assert.AreNotEqual(sut.Original.FirstName, "Ueli");
        }

        private class Fixture : FixtureBase<EntityTracker<Patient>>
        {
            public Fixture(Patient p = null)
            {
                Sut = new EntityTracker<Patient>(p ?? new PatientBuilder().Get());
            }
        }
    }
}
