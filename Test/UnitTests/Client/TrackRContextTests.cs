using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TestData;
using TestData.Builders;
using TestData.Entities;
using TrackR.Client;
using TrackR.Common;

namespace UnitTests.Client
{
    public class TrackRContextTests
    {
        [Test]
        public void Ctor_Called_PropertiesSetUp()
        {
            // * Arrange
            var fixture = new Fixture();

            // * Act
            var sut = fixture.Build();

            // * Assert
            Assert.IsNotNull(sut.BaseUri);
            Assert.IsFalse(sut.BaseUri.Host.IsNullOrWhiteSpace());
            Assert.AreEqual("http://localhost:3663/", sut.BaseUri.ToString());

            Assert.IsNotNull(sut.EntitySets);
        }

        [Test]
        public void Add_Called_EntityAddedAndSetCreated()
        {
            // * Arrange
            var fixture = new Fixture();
            var sut = fixture.Build();

            // * Act
            var patient = new PatientBuilder().Get();
            sut.Add(patient);

            // * Assert
            Assert.AreEqual(1, sut.EntitySets.Count);
            Assert.IsTrue(sut.EntitySets.First() is EntitySet<Patient>);
            Assert.AreEqual(ChangeState.Added, sut.EntitySets.First().EntitiesNonGeneric.First().State);
        }

        [Test]
        public void Remove_CalledAfterAdd_EntityGone()
        {
            // * Arrange
            var fixture = new Fixture();
            var sut = fixture.Build();

            // * Act
            var patient = new PatientBuilder().Get();
            sut.Add(patient);
            sut.Remove(patient);

            // * Assert
            Assert.AreEqual(1, sut.EntitySets.Count);
            Assert.AreEqual(0, sut.EntitySets.First().EntitiesNonGeneric.Count());
        }

        [Test]
        public void Remove_CalledAfterTrack_MarkedDeleted()
        {
            // * Arrange
            var fixture = new Fixture();
            var sut = fixture.Build();

            // * Act
            var patient = new PatientBuilder().WithoutAddress().Get();
            sut.Track(patient);
            sut.Remove(patient);

            // * Assert
            Assert.AreEqual(1, sut.EntitySets.Count);
            Assert.AreEqual(1, sut.EntitySets.First().EntitiesNonGeneric.Count());
            Assert.AreEqual(ChangeState.Deleted, sut.EntitySets.First().EntitiesNonGeneric.First().State);
        }

        [Test]
        public void Track_Called_ObjectGraphTracked()
        {
            // * Arrange
            var fixture = new Fixture();
            var sut = fixture.Build();

            // * Act
            var patient = new PatientBuilder().Get();
            sut.Track(patient);

            // * Assert
            Assert.AreEqual(2, sut.EntitySets.Count);
            Assert.IsTrue(sut.EntitySets.OfType<EntitySet<Address>>().Any());
            Assert.IsTrue(sut.EntitySets.OfType<EntitySet<Patient>>().Any());
            Assert.AreEqual(1, sut.EntitySets.OfType<EntitySet<Address>>().First().Entities.Count);
            Assert.AreEqual(1, sut.EntitySets.OfType<EntitySet<Patient>>().First().Entities.Count);
        }

        [Test]
        public void TrackMany_CalledWithCommonObjectsInGraph_OnlyDistinctAdded()
        {
            // * Arrange
            var fixture = new Fixture();
            var sut = fixture.Build();

            // * Act
            var patients = fixture.GetPatients(2);
            patients[1].Address = patients[0].Address;
            sut.TrackMany(patients);

            // * Assert
            Assert.AreEqual(2, sut.EntitySets.Count);
            Assert.IsTrue(sut.EntitySets.OfType<EntitySet<Address>>().Any());
            Assert.IsTrue(sut.EntitySets.OfType<EntitySet<Patient>>().Any());
            Assert.AreEqual(1, sut.EntitySets.OfType<EntitySet<Address>>().First().Entities.Count);
            Assert.AreEqual(2, sut.EntitySets.OfType<EntitySet<Patient>>().First().Entities.Count);
        }

        [Test]
        public void TrackMany_Called_AllObjectGraphsTracked()
        {
            // * Arrange
            var fixture = new Fixture();
            var sut = fixture.Build();

            // * Act
            var patients = fixture.GetPatients(5);
            sut.TrackMany(patients);

            // * Assert
            Assert.AreEqual(2, sut.EntitySets.Count);
            Assert.IsTrue(sut.EntitySets.OfType<EntitySet<Address>>().Any());
            Assert.IsTrue(sut.EntitySets.OfType<EntitySet<Patient>>().Any());
            Assert.AreEqual(5, sut.EntitySets.OfType<EntitySet<Patient>>().First().Entities.Count);
            Assert.AreEqual(5, sut.EntitySets.OfType<EntitySet<Address>>().First().Entities.Count);
        }

        [Test]
        public void UnTrack_CalledAfterAdd_EntityGone()
        {
            // * Arrange
            var fixture = new Fixture();
            var sut = fixture.Build();

            // * Act
            var patient = new PatientBuilder().Get();
            sut.Add(patient);
            sut.UnTrack(patient);

            // * Assert
            Assert.AreEqual(1, sut.EntitySets.Count);
            Assert.IsTrue(sut.EntitySets.OfType<EntitySet<Patient>>().Any());
            Assert.AreEqual(0, sut.EntitySets.OfType<EntitySet<Patient>>().First().Entities.Count);
        }

        [Test]
        public void UnTrack_CalledAfterTrack_EntityGone()
        {
            // * Arrange
            var fixture = new Fixture();
            var sut = fixture.Build();

            // * Act
            var patient = new PatientBuilder().Get();
            sut.Track(patient);
            sut.UnTrack(patient);

            // * Assert
            Assert.AreEqual(2, sut.EntitySets.Count);
            Assert.IsTrue(sut.EntitySets.OfType<EntitySet<Address>>().Any());
            Assert.IsTrue(sut.EntitySets.OfType<EntitySet<Patient>>().Any());
            Assert.AreEqual(1, sut.EntitySets.OfType<EntitySet<Address>>().First().Entities.Count);
            Assert.AreEqual(0, sut.EntitySets.OfType<EntitySet<Patient>>().First().Entities.Count);
        }


        [Test]
        public async Task SumitChangesAsync_Called_ServiceCalled()
        {
            // * Arrange
            var patient = new PatientBuilder().Get();
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new FlatJsonResolver(),
                TypeNameHandling = TypeNameHandling.Objects,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            };

            var changeSet = new ChangeSet { Entities = new List<EntityWrapper>() };
            var json = JsonConvert.SerializeObject(changeSet, settings);

            var fixture = new Fixture();
            fixture.MockHttpHandler
                .Setup(x => x.Send(It.IsAny<HttpRequestMessage>()))
                .Returns(() => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });
            var sut = fixture.Build();

            // * Act
            sut.Add(patient);
            await sut.SubmitChangesAsync();
            
            // * Assert
            fixture.MockHttpHandler.Verify(x => x.Send(It.IsAny<HttpRequestMessage>()), Times.Once);
        }

        [Test, ExpectedException(typeof(WebException))]
        public async Task SubmitChangesAsync_Returns404_Throws()
        {
            // * Arrange
            var patient = new PatientBuilder().Get();
            var fixture = new Fixture();
            fixture.MockHttpHandler
                .Setup(x => x.Send(It.IsAny<HttpRequestMessage>()))
                .Returns(() => new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("*TEST*") });
            var sut = fixture.Build();

            // * Act
            sut.Add(patient);
            await sut.SubmitChangesAsync();

            // * Assert
        }

        [Test]
        public void RejectChanges_Called_TrackedEntitiesResetAndFlaggedUnchanged()
        {
            // * Arrange
            var fixture = new Fixture();
            var sut = fixture.Build();

            // * Act
            var patient = new PatientBuilder().With(p => p.FirstName = "Kari").Get();
            sut.Track(patient);
            patient.FirstName = "Ueli";
            sut.RejectChanges();

            // * Assert
            Assert.AreEqual("Kari", patient.FirstName);
            Assert.IsTrue(sut.EntitySets.SelectMany(s => s.EntitiesNonGeneric).All(e => e.State == ChangeState.Unchanged));
        }

        [Test]
        public void RejectChanges_Called_AddedEntitiesGone()
        {
            // * Arrange
            var fixture = new Fixture();
            var sut = fixture.Build();

            // * Act
            var patient = new PatientBuilder().Get();
            sut.Add(patient);
            sut.RejectChanges();

            // * Assert
            Assert.AreEqual(1, sut.EntitySets.Count);
            Assert.IsTrue(sut.EntitySets.OfType<EntitySet<Patient>>().Any());
            Assert.AreEqual(0, sut.EntitySets.OfType<EntitySet<Patient>>().First().Entities.Count);
        }

        [Test]
        public void RejectChanged_Called_RemovedEntitiesUnchanged()
        {
            // * Arrange
            var fixture = new Fixture();
            var sut = fixture.Build();

            // * Act
            var patient = new PatientBuilder().Get();
            sut.Track(patient);
            sut.Remove(patient);
            sut.RejectChanges();

            // * Assert
            Assert.IsTrue(sut.EntitySets.SelectMany(s => s.EntitiesNonGeneric).All(e => e.State == ChangeState.Unchanged));
        }

        [Test]
        public void HasChanges_Vanilla_IsFalse()
        {
            // * Arrange
            var fixture = new Fixture();
            var sut = fixture.Build();

            // * Act
            // noop

            // * Assert
            Assert.IsFalse(sut.HasChanges);
        }

        [Test]
        public void HasChanges_TrackEntity_IsFalse()
        {
            // * Arrange
            var fixture = new Fixture();
            var sut = fixture.Build();

            // * Act
            var patient = new PatientBuilder().Get();
            sut.Track(patient);

            // * Assert
            Assert.IsFalse(sut.HasChanges);
        }

        [Test]
        public void HasChanges_EntityAdded_IsTrue()
        {
            // * Arrange
            var fixture = new Fixture();
            var sut = fixture.Build();

            // * Act
            var patient = new PatientBuilder().Get();
            sut.Add(patient);

            // * Assert
            Assert.IsTrue(sut.HasChanges);
        }

        [Test]
        public void HasChanges_EntityRemoved_IsTrue()
        {
            // * Arrange
            var fixture = new Fixture();
            var sut = fixture.Build();

            // * Act
            var patient = new PatientBuilder().Get();
            sut.Track(patient);
            sut.Remove(patient);

            // * Assert
            Assert.IsTrue(sut.HasChanges);
        }

        [Test]
        public void HasChanges_AddedRemoved_IsFalse()
        {
            // * Arrange
            var fixture = new Fixture();
            var sut = fixture.Build();

            // * Act
            var patient = new PatientBuilder().Get();
            sut.Add(patient);
            sut.Remove(patient);

            // * Assert
            Assert.IsFalse(sut.HasChanges);
        }

        [Test]
        public void HasChanges_TrackedEntityChanged_IsTrue()
        {
            // * Arrange
            var fixture = new Fixture();
            var sut = fixture.Build();

            // * Act
            var patient = new PatientBuilder().With(x => x.FirstName = "Kari").Get();
            sut.Track(patient);
            patient.FirstName = "Ueli";

            // * Assert
            Assert.IsTrue(sut.HasChanges);
        }

        [Test]
        public void Clear_Called_AllEntitiesAndSetsGone()
        {
            // * Arrange
            var fixture = new Fixture();
            var sut = fixture.Build();

            // * Act
            var patient = new PatientBuilder().Get();
            sut.Track(patient);
            sut.Clear();

            // * Assert
            Assert.AreEqual(0, sut.EntitySets.Count);
        }


        #region Fixtures & Fakes

        private class Fixture : FixtureBase<TrackRContext<Entity>>
        {
            public Mock<FakeHttpMessageHandler> MockHttpHandler { get; private set; }

            public Fixture()
            {
                MockHttpHandler = new Mock<FakeHttpMessageHandler> { CallBase = true };
            }

            public override TrackRContext<Entity> Build()
            {
                Sut = new TrackRContextFake(new Uri("http://localhost:3663/api/TrackR"), MockHttpHandler);
                return Sut;
            }

            public List<Patient> GetPatients(int amount)
            {
                var patients = new PatientBuilder().Get(amount).ToList();
                for (int i = 1; i <= amount; i++)
                {
                    patients[i - 1].Id = i;
                    patients[i - 1].Address = new AddressBuilder().Get();
                    patients[i - 1].Address.Id = i;
                    patients[i - 1].AddressId = i;
                }
                return patients;
            }
        }

        private class TrackRContextFake : TrackRContext<Entity>
        {
            private readonly Mock<FakeHttpMessageHandler> _clientHandler;

            public TrackRContextFake(Uri trackRUri, Mock<FakeHttpMessageHandler> clientHandler)
                : base(trackRUri)
            {
                _clientHandler = clientHandler;
            }

            protected override int GetId(object entity)
            {
                return (int)entity.GetType().GetProperty("Id").GetValue(entity);
            }
            protected override void SetId(object entity, int value)
            {
                entity.GetType().GetProperty("Id").SetValue(entity, value);
            }

            protected override HttpClient CreateHttpClient()
            {
                return new HttpClient(_clientHandler.Object);
            }
        }

        #endregion
    }
}
