using NUnit.Framework;
using System.Linq;
using TestData.Builders;
using TestData.Entities;
using TrackR.Client;
using TrackR.Common;

namespace UnitTests.Client
{
    [TestFixture]
    public class EntitySetTests
    {
        [Test]
        public void Ctor_Called_PropertiesSetUp()
        {
            // * Arrange

            // * Act
            var sut = new EntitySet<Patient>();

            // * Assert
            Assert.IsNotNull(sut.Entities);
            Assert.IsNotNull(sut.EntitiesNonGeneric);
            Assert.AreEqual(sut.Type, "TestData.Entities.Patient");
        }

        #region Generic

        [Test]
        public void Add_Called_EntityAdded()
        {
            // * Arrange
            var fixture = new Fixture();
            var sut = fixture.Build();
            var p = fixture.GetPatient().Get();

            // * Act
            sut.Add(p);

            // * Assert
            Assert.IsTrue(sut.Entities.Any());
            Assert.IsTrue(sut.EntitiesNonGeneric.Any());
            Assert.IsTrue(sut.Entities.First().State == ChangeState.Added);
        }

        [Test]
        public void Track_Called_EntityTracked()
        {
            // * Arrange
            var fixture = new Fixture();
            var sut = fixture.Build();
            var p = fixture.GetPatient().Get();

            // * Act
            sut.Track(p);

            // * Assert
            Assert.IsTrue(sut.Entities.Any());
            Assert.IsTrue(sut.EntitiesNonGeneric.Any());
            Assert.IsTrue(sut.Entities.First().State == ChangeState.Unchanged);
        }

        [Test]
        public void TrackAndEdited_Called_MarkedAsChanged()
        {
            // * Arrange
            var fixture = new Fixture();
            var sut = fixture.Build();
            var p = fixture.GetPatient().Get();

            // * Act
            sut.Track(p);
            p.LastName = "Doelorian";

            // * Assert
            Assert.IsTrue(sut.Entities.Any());
            Assert.IsTrue(sut.EntitiesNonGeneric.Any());
            Assert.IsTrue(sut.Entities.First().State == ChangeState.Changed);
        }

        [Test]
        public void Remove_Called_EntityMarkedAsRemoved()
        {
            // * Arrange
            var fixture = new Fixture();
            var sut = fixture.Build();
            var p = fixture.GetPatient().Get();
            sut.Track(p);

            // * Act
            sut.Remove(p);

            // * Assert
            Assert.IsTrue(sut.Entities.Any());
            Assert.IsTrue(sut.EntitiesNonGeneric.Any());
            Assert.IsTrue(sut.Entities.First().State == ChangeState.Deleted);
        }

        [Test]
        public void UnTrack_Called_EntityGone()
        {
            // * Arrange
            var fixture = new Fixture();
            var sut = fixture.Build();
            var p = fixture.GetPatient().Get();
            sut.Add(p);

            // * Act
            sut.UnTrack(p);

            // * Assert
            Assert.IsFalse(sut.Entities.Any());
            Assert.IsFalse(sut.EntitiesNonGeneric.Any());
        }

        [Test]
        public void Remove_CalledAfterAdd_EntityGone()
        {
            // * Arrange
            var fixture = new Fixture();
            var sut = fixture.Build();
            var p = fixture.GetPatient().Get();
            sut.Add(p);

            // * Act
            sut.Remove(p);

            // * Assert
            Assert.IsFalse(sut.Entities.Any());
            Assert.IsFalse(sut.EntitiesNonGeneric.Any());
        }

        #endregion

        #region Non-Generic

        [Test]
        public void AddEntity_Called_EntityAdded()
        {
            // * Arrange
            var fixture = new Fixture();
            var sut = fixture.BuildNonGeneric();
            var p = fixture.GetPatient().Get();

            // * Act
            sut.AddEntity(p);

            // * Assert
            Assert.IsTrue(sut.EntitiesNonGeneric.Any());
            Assert.IsTrue(sut.EntitiesNonGeneric.First().State == ChangeState.Added);
        }

        [Test]
        public void TrackEntity_Called_EntityTracked()
        {
            // * Arrange
            var fixture = new Fixture();
            var sut = fixture.BuildNonGeneric();
            var p = fixture.GetPatient().Get();

            // * Act
            sut.TrackEntity(p);

            // * Assert
            Assert.IsTrue(sut.EntitiesNonGeneric.Any());
            Assert.IsTrue(sut.EntitiesNonGeneric.First().State == ChangeState.Unchanged);
        }

        [Test]
        public void TrackEntityAndEdited_Called_MarkedAsChanged()
        {
            // * Arrange
            var fixture = new Fixture();
            var sut = fixture.BuildNonGeneric();
            var p = fixture.GetPatient().Get();

            // * Act
            sut.TrackEntity(p);
            p.LastName = "Doelorian";

            // * Assert
            Assert.IsTrue(sut.EntitiesNonGeneric.Any());
            Assert.IsTrue(sut.EntitiesNonGeneric.First().State == ChangeState.Changed);
        }

        [Test]
        public void RemoveEntity_Called_EntityMarkedAsRemoved()
        {
            // * Arrange
            var fixture = new Fixture();
            var sut = fixture.BuildNonGeneric();
            var p = fixture.GetPatient().Get();
            sut.TrackEntity(p);

            // * Act
            sut.RemoveEntity(p);

            // * Assert
            Assert.IsTrue(sut.EntitiesNonGeneric.Any());
            Assert.IsTrue(sut.EntitiesNonGeneric.First().State == ChangeState.Deleted);
        }

        [Test]
        public void UnTrackEntity_Called_EntityGone()
        {
            // * Arrange
            var fixture = new Fixture();
            var sut = fixture.BuildNonGeneric();
            var p = fixture.GetPatient().Get();
            sut.AddEntity(p);

            // * Act
            sut.UnTrackEntity(p);

            // * Assert
            Assert.IsFalse(sut.EntitiesNonGeneric.Any());
        }

        [Test]
        public void RemoveEntity_CalledAfterAdd_EntityGone()
        {
            // * Arrange
            var fixture = new Fixture();
            var sut = fixture.BuildNonGeneric();
            var p = fixture.GetPatient().Get();
            sut.AddEntity(p);

            // * Act
            sut.RemoveEntity(p);

            // * Assert
            Assert.IsFalse(sut.EntitiesNonGeneric.Any());
        }

        #endregion


        private class Fixture
        {
            private EntitySet<Patient> Sut { get; set; }

            public Fixture()
            {
                Sut = new EntitySet<Patient>();
            }

            public EntitySet<Patient> Build()
            {
                return Sut;
            }

            public EntitySet BuildNonGeneric()
            {
                return Sut;
            }

            public PatientBuilder GetPatient()
            {
                return new PatientBuilder();
            }
        }
    }
}

