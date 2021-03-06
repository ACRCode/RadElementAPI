﻿using Microsoft.EntityFrameworkCore;
using Moq;
using RadElement.Core.Domain;
using RadElement.Core.DTO;
using RadElement.Core.Data;
using RadElement.Service.Tests.Mocks.Data;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Xunit;
using Microsoft.EntityFrameworkCore.Infrastructure;
using RadElement.Core.Infrastructure;

namespace RadElement.Service.Tests
{
    public class PersonServiceShould
    {
        /// <summary>
        /// The person service
        /// </summary>
        private readonly PersonService service;

        /// <summary>
        /// The mock RAD element context
        /// </summary>
        private readonly Mock<RadElementDbContext> mockRadElementContext;

        /// <summary>
        /// The mock logger
        /// </summary>
        private readonly Mock<ILogger> mockLogger;

        private const string connectionString = "server=localhost;user id=root;password=root;persistsecurityinfo=True;database=radelement;Convert Zero Datetime=True";
        private const string personNotFoundMessage = "No such person with id '{0}'.";
        private const string personNotFoundMessageWithSearchMessage = "No such person with keyword '{0}'.";
        private const string invalidSearchMessage = "Keyword given is invalid.";
        private const string personInvalidMessage = "Person fields are invalid.";
        private const string personUpdateMessage = "Person with id '{0}' is updated.";
        private const string personDeletedMessage = "Person with id '{0}' is deleted.";

        /// <summary>
        /// Initializes a new instance of the <see cref="PersonServiceShould"/> class.
        /// </summary>
        public PersonServiceShould()
        {
            mockRadElementContext = new Mock<RadElementDbContext>();
            mockLogger = new Mock<ILogger>();

            service = new PersonService(mockRadElementContext.Object, mockLogger.Object);
        }

        #region GetPersons

        [Fact]
        public async void GetPersonsShouldThrowInternalServerErrorForExceptions()
        {
            IntializeMockData(false);
            var result = await service.GetPersons();

            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.Equal(HttpStatusCode.InternalServerError, result.Code);
        }

        [Fact]
        public async void GetPersonsShouldReturnAllPersons()
        {
            IntializeMockData(true);
            var result = await service.GetPersons();

            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.IsType<List<Person>>(result.Value);
            Assert.Equal(HttpStatusCode.OK, result.Code);
        }

        #endregion

        #region GetPerson

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public async void GetPersonShouldThrowInternalServerErrorForExceptions(int personId)
        {
            IntializeMockData(false);
            var result = await service.GetPerson(personId);

            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.Equal(HttpStatusCode.InternalServerError, result.Code);
        }

        [Theory]
        [InlineData(7)]
        [InlineData(8)]
        public async void GetPersonShouldReturnNotFoundIfDoesnotExists(int personId)
        {
            IntializeMockData(true);
            var result = await service.GetPerson(personId);

            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.IsType<string>(result.Value);
            Assert.Equal(HttpStatusCode.NotFound, result.Code);
            Assert.Equal(string.Format(personNotFoundMessage, personId), result.Value);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public async void GetPersonShouldReturnPersonBasedOnPersonId(int personId)
        {
            IntializeMockData(true);
            var result = await service.GetPerson(personId);

            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.IsType<Person>(result.Value);
            Assert.Equal(HttpStatusCode.OK, result.Code);
        }

        #endregion

        #region SearchPerson

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async void SearchPersonShouldReturnBadRequestIfSearchKeywordIsInvalid(string searchKeyword)
        {
            IntializeMockData(true);
            var result = await service.SearchPersons(searchKeyword);

            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.IsType<string>(result.Value);
            Assert.Equal(HttpStatusCode.BadRequest, result.Code);
            Assert.Equal(string.Format(invalidSearchMessage, searchKeyword), result.Value);
        }

        [Theory]
        [InlineData("test")]
        [InlineData("test1")]
        public async void SearchPersonShouldReturnNotFoundIfSearchKeywordDoesnotExists(string searchKeyword)
        {
            IntializeMockData(true);
            var result = await service.SearchPersons(searchKeyword);

            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.IsType<string>(result.Value);
            Assert.Equal(HttpStatusCode.NotFound, result.Code);
            Assert.Equal(string.Format(personNotFoundMessageWithSearchMessage, searchKeyword), result.Value);
        }

        [Theory]
        [InlineData("Tumuor")]
        public async void SearchPersonShouldReturnThrowInternalServerErrorForExceptions(string searchKeyword)
        {
            IntializeMockData(false);
            var result = await service.SearchPersons(searchKeyword);

            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.Equal(HttpStatusCode.InternalServerError, result.Code);
        }

        [Theory]
        [InlineData("Adam")]
        public async void SearchPersonShouldReturnPersonssIfSearchedPersonExists(string searchKeyword)
        {
            IntializeMockData(true);
            var result = await service.SearchPersons(searchKeyword);

            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.IsType<List<Person>>(result.Value);
            Assert.Equal(HttpStatusCode.OK, result.Code);
        }

        #endregion

        #region CreatePerson

        [Theory]
        [InlineData(null)]
        public async void CreatePersonShouldReturnBadRequestIfPersonDetailsAreInvalid(CreateUpdatePerson person)
        {
            IntializeMockData(true);
            var result = await service.CreatePerson(person);

            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.IsType<string>(result.Value);
            Assert.Equal(HttpStatusCode.BadRequest, result.Code);
            Assert.Equal(personInvalidMessage, result.Value);
        }

        [Theory]
        [InlineData("Adam", "Orcidr1")]
        [InlineData("Mike", "Orcidr2")]
        public async void CreatePersonShouldReturnThrowInternalServerErrorForExceptions(string name, string orcid)
        {
            var person = new CreateUpdatePerson();
            person.Name = name;
            person.Orcid = orcid;

            IntializeMockData(false);
            var result = await service.CreatePerson(person);

            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.Equal(HttpStatusCode.InternalServerError, result.Code);
        }

        [Theory]
        [InlineData("Charles E. Kahn, Jr., MD, MS")]
        [InlineData("Woojin Kim, MD")]
        public async void CreatePersonShouldReturnUpdatedPersonAndIdIfPersonDetailsAlreadyExists(string name)
        {
            var person = new CreateUpdatePerson();
            person.Name = name;

            IntializeMockData(true);
            var result = await service.CreatePerson(person);

            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.IsType<PersonIdDetails>(result.Value);
            Assert.Equal(HttpStatusCode.OK, result.Code);
        }

        [Theory]
        [InlineData("Adam", "Orcidr1")]
        [InlineData("Mike", "Orcidr2")]
        public async void CreatePersonShouldReturnPersonIdIfPersonDetailsDoesNotExists(string name, string orcid)
        {
            var person = new CreateUpdatePerson();
            person.Name = name;
            person.Orcid = orcid;

            IntializeMockData(true);
            var result = await service.CreatePerson(person);

            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.IsType<PersonIdDetails>(result.Value);
            Assert.Equal(HttpStatusCode.Created, result.Code);
        }

        #endregion

        #region UpdatePerson

        [Theory]
        [InlineData(1, null)]
        [InlineData(2, null)]
        public async void UpdatePersonShouldReturnBadRequestIfPersonDetailsAreInvalid(int personId, CreateUpdatePerson person)
        {
            IntializeMockData(true);
            var result = await service.UpdatePerson(personId, person);

            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.IsType<string>(result.Value);
            Assert.Equal(HttpStatusCode.BadRequest, result.Code);
            Assert.Equal(personInvalidMessage, result.Value);
        }

        [Theory]
        [InlineData(60)]
        [InlineData(70)]
        public async void UpdatePersonShouldReturnENotFoundIfPersonDoesNotExists(int personId)
        {
            var person = new CreateUpdatePerson();
            person.Name = "Tumuor";
            person.Orcid = "Orcidr";

            IntializeMockData(true);
            var result = await service.UpdatePerson(personId, person);

            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.IsType<string>(result.Value);
            Assert.Equal(HttpStatusCode.NotFound, result.Code);
            Assert.Equal(string.Format(personNotFoundMessage, personId), result.Value);
        }

        [Theory]
        [InlineData(2)]
        public async void UpdatePersonShouldReturnThrowInternalServerErrorForExceptions(int personId)
        {
            var person = new CreateUpdatePerson();
            person.Name = "Tumuor";
            person.Orcid = "Orcidr";

            IntializeMockData(false);
            var result = await service.UpdatePerson(personId, person);

            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.Equal(HttpStatusCode.InternalServerError, result.Code);
        }

        [Theory]
        [InlineData(2)]
        public async void UpdatePersonShouldReturnPersonIdIfPersonDetailsAreValid(int personId)
        {
            var person = new CreateUpdatePerson();
            person.Name = "Tumuor";
            person.Orcid = "Orcidr";

            IntializeMockData(true);
            var result = await service.UpdatePerson(personId, person);

            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.Equal(HttpStatusCode.OK, result.Code);
            Assert.Equal(string.Format(personUpdateMessage, personId), result.Value);
        }

        #endregion

        #region DeletePerson

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        public async void DeletePersonShouldReturnNotFoundIfPersontIdIsInvalid(int personId)
        {
            IntializeMockData(true);
            var result = await service.DeletePerson(personId);

            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.IsType<string>(result.Value);
            Assert.Equal(HttpStatusCode.NotFound, result.Code);
            Assert.Equal(string.Format(personNotFoundMessage, personId), result.Value);
        }

        [Theory]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        public async void DeletePersonShouldThrowInternalServerErrorForExceptions(int personId)
        {
            IntializeMockData(false);
            var result = await service.DeletePerson(personId);

            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.Equal(HttpStatusCode.InternalServerError, result.Code);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public async void DeletePersonShouldDeletePersonIfPersonIdIsValid(int personId)
        {
            IntializeMockData(true);
            var result = await service.DeletePerson(personId);

            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.IsType<string>(result.Value);
            Assert.Equal(HttpStatusCode.OK, result.Code);
            Assert.Equal(string.Format(personDeletedMessage, personId), result.Value);
        }

        #endregion

        #region Private Methods

        private void IntializeMockData(bool mockDatabaseData)
        {
            if (mockDatabaseData)
            {
                var mockElement = new Mock<DbSet<Element>>();
                mockElement.As<IQueryable<Element>>().Setup(m => m.Provider).Returns(MockDataContext.elementsDB.Provider);
                mockElement.As<IQueryable<Element>>().Setup(m => m.Expression).Returns(MockDataContext.elementsDB.Expression);
                mockElement.As<IQueryable<Element>>().Setup(m => m.ElementType).Returns(MockDataContext.elementsDB.ElementType);
                mockElement.As<IQueryable<Element>>().Setup(m => m.GetEnumerator()).Returns(MockDataContext.elementsDB.GetEnumerator());

                var mockSet = new Mock<DbSet<ElementSet>>();
                mockSet.As<IQueryable<ElementSet>>().Setup(m => m.Provider).Returns(MockDataContext.elementSetDb.Provider);
                mockSet.As<IQueryable<ElementSet>>().Setup(m => m.Expression).Returns(MockDataContext.elementSetDb.Expression);
                mockSet.As<IQueryable<ElementSet>>().Setup(m => m.ElementType).Returns(MockDataContext.elementSetDb.ElementType);
                mockSet.As<IQueryable<ElementSet>>().Setup(m => m.GetEnumerator()).Returns(MockDataContext.elementSetDb.GetEnumerator());

                var mockElementSetRef = new Mock<DbSet<ElementSetRef>>();
                mockElementSetRef.As<IQueryable<ElementSetRef>>().Setup(m => m.Provider).Returns(MockDataContext.elementSetRefDb.Provider);
                mockElementSetRef.As<IQueryable<ElementSetRef>>().Setup(m => m.Expression).Returns(MockDataContext.elementSetRefDb.Expression);
                mockElementSetRef.As<IQueryable<ElementSetRef>>().Setup(m => m.ElementType).Returns(MockDataContext.elementSetRefDb.ElementType);
                mockElementSetRef.As<IQueryable<ElementSetRef>>().Setup(m => m.GetEnumerator()).Returns(MockDataContext.elementSetRefDb.GetEnumerator());

                var mockElementValue = new Mock<DbSet<ElementValue>>();
                mockElementValue.As<IQueryable<ElementValue>>().Setup(m => m.Provider).Returns(MockDataContext.elementValueDb.Provider);
                mockElementValue.As<IQueryable<ElementValue>>().Setup(m => m.Expression).Returns(MockDataContext.elementValueDb.Expression);
                mockElementValue.As<IQueryable<ElementValue>>().Setup(m => m.ElementType).Returns(MockDataContext.elementValueDb.ElementType);
                mockElementValue.As<IQueryable<ElementValue>>().Setup(m => m.GetEnumerator()).Returns(MockDataContext.elementValueDb.GetEnumerator());

                var mockPerson = new Mock<DbSet<Person>>();
                mockPerson.As<IQueryable<Person>>().Setup(m => m.Provider).Returns(MockDataContext.personDb.Provider);
                mockPerson.As<IQueryable<Person>>().Setup(m => m.Expression).Returns(MockDataContext.personDb.Expression);
                mockPerson.As<IQueryable<Person>>().Setup(m => m.ElementType).Returns(MockDataContext.personDb.ElementType);
                mockPerson.As<IQueryable<Person>>().Setup(m => m.GetEnumerator()).Returns(MockDataContext.personDb.GetEnumerator());

                var mockPersonElementRef = new Mock<DbSet<PersonRoleElementRef>>();
                mockPersonElementRef.As<IQueryable<PersonRoleElementRef>>().Setup(m => m.Provider).Returns(MockDataContext.personElementRefDb.Provider);
                mockPersonElementRef.As<IQueryable<PersonRoleElementRef>>().Setup(m => m.Expression).Returns(MockDataContext.personElementRefDb.Expression);
                mockPersonElementRef.As<IQueryable<PersonRoleElementRef>>().Setup(m => m.ElementType).Returns(MockDataContext.personElementRefDb.ElementType);
                mockPersonElementRef.As<IQueryable<PersonRoleElementRef>>().Setup(m => m.GetEnumerator()).Returns(MockDataContext.personElementRefDb.GetEnumerator());

                var mockPersonElementSetRef = new Mock<DbSet<PersonRoleElementSetRef>>();
                mockPersonElementSetRef.As<IQueryable<PersonRoleElementSetRef>>().Setup(m => m.Provider).Returns(MockDataContext.personElementSetRefDb.Provider);
                mockPersonElementSetRef.As<IQueryable<PersonRoleElementSetRef>>().Setup(m => m.Expression).Returns(MockDataContext.personElementSetRefDb.Expression);
                mockPersonElementSetRef.As<IQueryable<PersonRoleElementSetRef>>().Setup(m => m.ElementType).Returns(MockDataContext.personElementSetRefDb.ElementType);
                mockPersonElementSetRef.As<IQueryable<PersonRoleElementSetRef>>().Setup(m => m.GetEnumerator()).Returns(MockDataContext.personElementSetRefDb.GetEnumerator());

                mockRadElementContext.Setup(c => c.Element).Returns(mockElement.Object);
                mockRadElementContext.Setup(c => c.ElementSet).Returns(mockSet.Object);
                mockRadElementContext.Setup(c => c.ElementSetRef).Returns(mockElementSetRef.Object);
                mockRadElementContext.Setup(c => c.ElementValue).Returns(mockElementValue.Object);
                mockRadElementContext.Setup(c => c.Person).Returns(mockPerson.Object);
                mockRadElementContext.Setup(c => c.PersonRoleElementRef).Returns(mockPersonElementRef.Object);
                mockRadElementContext.Setup(c => c.PersonRoleElementSetRef).Returns(mockPersonElementSetRef.Object);
            }

            var mockConfigurationManager = new Mock<IConfigurationManager>();
            var options = new DbContextOptionsBuilder<RadElementDbContext>().UseMySql(connectionString).Options;

            mockRadElementContext.Setup(c => c.Database).Returns(new DatabaseFacade(new RadElementDbContext(options, mockConfigurationManager.Object)));
        }

        #endregion
    }
}
