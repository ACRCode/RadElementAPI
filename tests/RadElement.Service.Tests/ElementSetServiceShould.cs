﻿using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Moq;
using RadElement.Core.Domain;
using RadElement.Core.DTO;
using RadElement.Core.Data;
using RadElement.Core.Profile;
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
    public class ElementSetServiceShould
    {
        /// <summary>
        /// The service
        /// </summary>
        private readonly ElementSetService service;

        /// <summary>
        /// The mock RAD element context
        /// </summary>
        private readonly Mock<RadElementDbContext> mockRadElementContext;

        /// <summary>
        /// The mock logger
        /// </summary>
        private readonly Mock<ILogger> mockLogger;

        /// <summary>
        /// The mapper
        /// </summary>
        private readonly IMapper mapper;

        private const string connectionString = "server=localhost;user id=root;password=root;persistsecurityinfo=True;database=radelement;Convert Zero Datetime=True";
        private const string setNotFoundMessage = "No such set with id '{0}'.";
        private const string setNotFoundMessageWithSearchMessage = "No such set with keyword '{0}'.";
        private const string setInvalidMessage = "Set fileds are invalid.";
        private const string invalidSearchMessage = "Keyword given is invalid.";
        private const string setUpdatedMessage = "Set with id '{0}' is updated.";
        private const string setDeletedMessage = "Set with id '{0}' is deleted.";

        /// <summary>
        /// Initializes a new instance of the <see cref="ElementSetServiceShould"/> class.
        /// </summary>
        public ElementSetServiceShould()
        {
            mockRadElementContext = new Mock<RadElementDbContext>();
            mockLogger = new Mock<ILogger>();

            var elementSetProfile = new ElementSetProfile();
            var personProfileProfile = new PersonProfile();
            var organizationProfileProfile = new OrganizationProfile();

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(elementSetProfile);
                cfg.AddProfile(personProfileProfile);
                cfg.AddProfile(organizationProfileProfile);
            });

            mapper = new Mapper(mapperConfig);
            service = new ElementSetService(mockRadElementContext.Object, mapper, mockLogger.Object);
        }

        #region GetSets

        [Fact]
        public async void GetSetsShouldThrowInternalServerErrorForExceptions()
        {
            IntializeMockData(false);
            var result = await service.GetSets();
            
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.Equal(HttpStatusCode.InternalServerError, result.Code);
        }

        [Fact]
        public async void GetSetsShouldReturnAllSets()
        {
            IntializeMockData(true);
            var result = await service.GetSets();

            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.IsType<List<ElementSet>>(result.Value);
            Assert.Equal(HttpStatusCode.OK, result.Code);
        }

        #endregion

        #region GetSet By SetId

        [Theory]
        [InlineData("RDES53")]
        [InlineData("RDES66")]
        public async void GetSetByIdShouldThrowInternalServerErrorForExceptions(string setId)
        {
            IntializeMockData(false);
            var result = await service.GetSet(setId);

            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.Equal(HttpStatusCode.InternalServerError, result.Code);
        }

        [Theory]
        [InlineData("RD1")]
        [InlineData("RD2")]
        public async void GetSetByIdShouldReturnNotFoundIfDoesnotExists(string setId)
        {
            IntializeMockData(true);
            var result = await service.GetSet(setId);

            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.IsType<string>(result.Value);
            Assert.Equal(HttpStatusCode.NotFound, result.Code);
            Assert.Equal(string.Format(setNotFoundMessage, setId), result.Value);
        }

        [Theory]
        [InlineData("RDES53")]
        [InlineData("RDES66")]
        public async void GetSetByIdShouldReturnSetsBasedOnSetId(string setId)
        {
            IntializeMockData(true);
            var result = await service.GetSet(setId);

            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.IsType<ElementSetDetails>(result.Value);
            Assert.Equal(HttpStatusCode.OK, result.Code);
        }

        #endregion

        #region SearchSet

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async void SearchSetShouldReturnBadRequestIfSearchKeywordIsInvalid(string searchKeyword)
        {
            IntializeMockData(true);
            var result = await service.SearchSets(searchKeyword);

            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.IsType<string>(result.Value);
            Assert.Equal(HttpStatusCode.BadRequest, result.Code);
            Assert.Equal(string.Format(invalidSearchMessage, searchKeyword), result.Value);
        }

        [Theory]
        [InlineData("test")]
        [InlineData("test1")]
        public async void SearchSetShouldReturnEmpySetIfSearchKeywordDoesnotExists(string searchKeyword)
        {
            IntializeMockData(true);
            var result = await service.SearchSets(searchKeyword);

            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.IsType<string>(result.Value);
            Assert.Equal(HttpStatusCode.NotFound, result.Code);
            Assert.Equal(string.Format(setNotFoundMessageWithSearchMessage, searchKeyword), result.Value);
        }
    
        [Theory]
        [InlineData("Pulmonary")]
        [InlineData("Kimberly")]
        public async void GetSetShouldReturnThrowInternalServerErrorForExceptions(string searchKeyword)
        {
            IntializeMockData(false);
            var result = await service.SearchSets(searchKeyword);

            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.Equal(HttpStatusCode.InternalServerError, result.Code);
        }

        [Theory]
        [InlineData("Tumor")]
        [InlineData("Tissue")]
        public async void GetSetShouldReturnSetIfSearchedElementExists(string searchKeyword)
        {
            IntializeMockData(true);
            var result = await service.SearchSets(searchKeyword);

            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.IsType<List<ElementSetDetails>>(result.Value);
            Assert.Equal(HttpStatusCode.OK, result.Code);
        }

        #endregion

        #region CreateSet

        [Theory]
        [InlineData(null)]
        public async void CreateSetShouldReturnBadRequestIfSetIsInvalid(CreateUpdateSet set)
        {
            IntializeMockData(true);
            var result = await service.CreateSet(set);

            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.IsType<string>(result.Value);
            Assert.Equal(HttpStatusCode.BadRequest, result.Code);
            Assert.Equal(setInvalidMessage, result.Value);
        }

        [Theory]
        [InlineData("Tumuor1", "Tumuor2", "Tumuor3")]
        [InlineData("Sinus1", "Sinus2", "Sinus3")]
        public async void CreateSetShouldThrowInternalServerErrorForExceptions(string moduleName, string contactName, string description)
        {
            var set = new CreateUpdateSet();
            set.Name = moduleName;
            set.ContactName = contactName;
            set.Description = description;
            set.ReferencesRef = new List<int> { 1, 2, 3 };
            set.ImagesRef = new List<int> { 1, 2, 3 };
            set.Persons = new List<PersonDetails>() {
                new PersonDetails { PersonId = 1, Roles = new List<PersonRole> { PersonRole.Author, PersonRole.Contributor } },
                new PersonDetails { PersonId = 2, Roles = new List<PersonRole> { } }
            };
            set.Organizations = new List<OrganizationDetails>() {
                new OrganizationDetails { OrganizationId = 1, Roles = new List<OrganizationRole> { OrganizationRole.Author, OrganizationRole.Contributor } },
                new OrganizationDetails { OrganizationId = 2, Roles = new List<OrganizationRole> { } }
            };

            IntializeMockData(false);
            var result = await service.CreateSet(set);

            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.Equal(HttpStatusCode.InternalServerError, result.Code);
        }

        [Theory]
        [InlineData("Tumuor1", "Tumuor2", "Tumuor3")]
        [InlineData("Sinus1", "Sinus2", "Sinus3")]
        public async void CreateSetShouldReturnSetIdIfSetIsValid(string moduleName, string contactName, string description)
        {
            var set = new CreateUpdateSet();
            set.Name = moduleName;
            set.ContactName = contactName;
            set.Description = description;
            set.ReferencesRef = new List<int> { 1, 2, 3 };
            set.ImagesRef = new List<int> { 1, 2, 3 };
            set.Persons = new List<PersonDetails>() {
                new PersonDetails { PersonId = 1, Roles = new List<PersonRole> { PersonRole.Author, PersonRole.Contributor } },
                new PersonDetails { PersonId = 2, Roles = new List<PersonRole> { } }
            };
            set.Organizations = new List<OrganizationDetails>() {
                new OrganizationDetails { OrganizationId = 1, Roles = new List<OrganizationRole> { OrganizationRole.Author, OrganizationRole.Contributor } },
                new OrganizationDetails { OrganizationId = 2, Roles = new List<OrganizationRole> { } }
            };
            set.IndexCodeReferences = new List<int>() { 1, 2, 3};
            set.Specialties = new List<SpecialtyValue>()
            {
                new SpecialtyValue { Value = "BR" },
                new SpecialtyValue { Value = "CA" },
                new SpecialtyValue { Value = "CH" }
            };

            IntializeMockData(true);
            var result = await service.CreateSet(set);

            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.IsType<SetIdDetails>(result.Value);
            Assert.Equal(HttpStatusCode.Created, result.Code);
        }

        #endregion

        #region UpdateSet

        [Theory]
        [InlineData("RDES53", null)]
        public async void UpdateSetShouldReturnBadRequestIfSetIsInvalid(string setId, CreateUpdateSet set)
        {
            IntializeMockData(true);
            var result = await service.UpdateSet(setId, set);

            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.IsType<string>(result.Value);
            Assert.Equal(HttpStatusCode.BadRequest, result.Code);
            Assert.Equal(setInvalidMessage, result.Value);
        }

        [Theory]
        [InlineData("RDES53", "Tumuor1", "Tumuor2", "Tumuor3")]
        [InlineData("RDES66", "Sinus1", "Sinus2", "Sinus3")]
        public async void UpdateSetShouldThrowInternalServerErrorForExceptions(string setId, string moduleName, string contactName, string description)
        {
            var set = new CreateUpdateSet();
            set.Name = moduleName;
            set.ContactName = contactName;
            set.Description = description;
            set.ReferencesRef = new List<int> { 1, 2, 3 };
            set.ImagesRef = new List<int> { 1, 2, 3 };

            IntializeMockData(false);
            var result = await service.UpdateSet(setId, set);

            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.Equal(HttpStatusCode.InternalServerError, result.Code);
        }

        [Theory]
        [InlineData("RD1")]
        [InlineData("RD2")]
        public async void UpdateSetShouldReturnNotFoundIfDoesnotExists(string setId)
        {
            var set = new CreateUpdateSet();
            set.Name = "name";
            set.ContactName = "contact";
            set.Description = "description";
            set.ReferencesRef = new List<int> { 1, 2, 3 };
            set.ImagesRef = new List<int> { 1, 2, 3 };

            IntializeMockData(true);
            var result = await service.UpdateSet(setId, set);

            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.IsType<string>(result.Value);
            Assert.Equal(HttpStatusCode.NotFound, result.Code);
            Assert.Equal(string.Format(setNotFoundMessage, setId), result.Value);
        }

        [Theory]
        [InlineData("RDES53", "Tumuor1", "Tumuor2", "Tumuor3")]
        [InlineData("RDES66", "Sinus1", "Sinus2", "Sinus3")]
        public async void UpdateSetShouldReturnSetIdIfSetIsValid(string setId, string moduleName, string contactName, string description)
        {
            var set = new CreateUpdateSet();
            set.Name = moduleName;
            set.ContactName = contactName;
            set.Description = description;
            set.ReferencesRef = new List<int> { 1, 2, 3 };
            set.ImagesRef = new List<int> { 1, 2, 3 };
            set.Specialties = new List<SpecialtyValue>()
            {
                new SpecialtyValue { Value = "BR" },
                new SpecialtyValue { Value = "CA" },
                new SpecialtyValue { Value = "CH" }
            };
            set.Persons = new List<PersonDetails>() {
                new PersonDetails { PersonId = 1, Roles = new List<PersonRole> { PersonRole.Author, PersonRole.Contributor } },
                new PersonDetails { PersonId = 2, Roles = new List<PersonRole> { } }
            };
            set.Organizations = new List<OrganizationDetails>() {
                new OrganizationDetails { OrganizationId = 1, Roles = new List<OrganizationRole> { OrganizationRole.Author, OrganizationRole.Contributor } },
                new OrganizationDetails { OrganizationId = 2, Roles = new List<OrganizationRole> { } }
            };

            IntializeMockData(true);
            var result = await service.UpdateSet(setId, set);

            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.IsType<string>(result.Value);
            Assert.Equal(HttpStatusCode.OK, result.Code);
            Assert.Equal(string.Format(setUpdatedMessage, setId), result.Value);
        }

        #endregion

        #region DeleteSet

        [Theory]
        [InlineData("RDES100")]
        [InlineData("RDES200")]
        public async void DeleteSetShouldReturnNotFoundIfSetIdDoesNotExists(string setId)
        {
            IntializeMockData(true);
            var result = await service.DeleteSet(setId);

            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.IsType<string>(result.Value);
            Assert.Equal(HttpStatusCode.NotFound, result.Code);
            Assert.Equal(string.Format(setNotFoundMessage, setId), result.Value);
        }

        [Theory]
        [InlineData("RDES53")]
        [InlineData("RDES66")]
        public async void DeleteSetShouldThrowInternalServerErrorForExceptions(string setId)
        {
            IntializeMockData(false);
            var result = await service.DeleteSet(setId);

            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.Equal(HttpStatusCode.InternalServerError, result.Code);
        }

        [Theory]
        [InlineData("RDES53")]
        [InlineData("RDES66")]
        public async void DeleteSetShouldDeleteSetIfSetIdIsValid(string setId)
        {
            IntializeMockData(true);
            var result = await service.DeleteSet(setId);

            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.IsType<string>(result.Value);
            Assert.Equal(HttpStatusCode.OK, result.Code);
            Assert.Equal(string.Format(setDeletedMessage, setId), result.Value);
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

                var mockIndexCodeSystem = new Mock<DbSet<IndexCodeSystem>>();
                mockIndexCodeSystem.As<IQueryable<IndexCodeSystem>>().Setup(m => m.Provider).Returns(MockDataContext.indexCodeSystemDb.Provider);
                mockIndexCodeSystem.As<IQueryable<IndexCodeSystem>>().Setup(m => m.Expression).Returns(MockDataContext.indexCodeSystemDb.Expression);
                mockIndexCodeSystem.As<IQueryable<IndexCodeSystem>>().Setup(m => m.ElementType).Returns(MockDataContext.indexCodeSystemDb.ElementType);
                mockIndexCodeSystem.As<IQueryable<IndexCodeSystem>>().Setup(m => m.GetEnumerator()).Returns(MockDataContext.indexCodeSystemDb.GetEnumerator());

                var mockIndexCode = new Mock<DbSet<IndexCode>>();
                mockIndexCode.As<IQueryable<IndexCode>>().Setup(m => m.Provider).Returns(MockDataContext.indexCodeDb.Provider);
                mockIndexCode.As<IQueryable<IndexCode>>().Setup(m => m.Expression).Returns(MockDataContext.indexCodeDb.Expression);
                mockIndexCode.As<IQueryable<IndexCode>>().Setup(m => m.ElementType).Returns(MockDataContext.indexCodeDb.ElementType);
                mockIndexCode.As<IQueryable<IndexCode>>().Setup(m => m.GetEnumerator()).Returns(MockDataContext.indexCodeDb.GetEnumerator());

                var mockIndexCodeElementRef = new Mock<DbSet<IndexCodeElementRef>>();
                mockIndexCodeElementRef.As<IQueryable<IndexCodeElementRef>>().Setup(m => m.Provider).Returns(MockDataContext.indexCodeElementDb.Provider);
                mockIndexCodeElementRef.As<IQueryable<IndexCodeElementRef>>().Setup(m => m.Expression).Returns(MockDataContext.indexCodeElementDb.Expression);
                mockIndexCodeElementRef.As<IQueryable<IndexCodeElementRef>>().Setup(m => m.ElementType).Returns(MockDataContext.indexCodeElementDb.ElementType);
                mockIndexCodeElementRef.As<IQueryable<IndexCodeElementRef>>().Setup(m => m.GetEnumerator()).Returns(MockDataContext.indexCodeElementDb.GetEnumerator());

                var mockIndexCodeElementSetRef = new Mock<DbSet<IndexCodeElementSetRef>>();
                mockIndexCodeElementSetRef.As<IQueryable<IndexCodeElementSetRef>>().Setup(m => m.Provider).Returns(MockDataContext.indexCodeElementSetDb.Provider);
                mockIndexCodeElementSetRef.As<IQueryable<IndexCodeElementSetRef>>().Setup(m => m.Expression).Returns(MockDataContext.indexCodeElementSetDb.Expression);
                mockIndexCodeElementSetRef.As<IQueryable<IndexCodeElementSetRef>>().Setup(m => m.ElementType).Returns(MockDataContext.indexCodeElementSetDb.ElementType);
                mockIndexCodeElementSetRef.As<IQueryable<IndexCodeElementSetRef>>().Setup(m => m.GetEnumerator()).Returns(MockDataContext.indexCodeElementSetDb.GetEnumerator());

                var mockIndexCodeElementValueRef = new Mock<DbSet<IndexCodeElementValueRef>>();
                mockIndexCodeElementValueRef.As<IQueryable<IndexCodeElementValueRef>>().Setup(m => m.Provider).Returns(MockDataContext.indexCodeElementValueDb.Provider);
                mockIndexCodeElementValueRef.As<IQueryable<IndexCodeElementValueRef>>().Setup(m => m.Expression).Returns(MockDataContext.indexCodeElementValueDb.Expression);
                mockIndexCodeElementValueRef.As<IQueryable<IndexCodeElementValueRef>>().Setup(m => m.ElementType).Returns(MockDataContext.indexCodeElementValueDb.ElementType);
                mockIndexCodeElementValueRef.As<IQueryable<IndexCodeElementValueRef>>().Setup(m => m.GetEnumerator()).Returns(MockDataContext.indexCodeElementValueDb.GetEnumerator());

                var mockOrganization = new Mock<DbSet<Organization>>();
                mockOrganization.As<IQueryable<Organization>>().Setup(m => m.Provider).Returns(MockDataContext.organizationDb.Provider);
                mockOrganization.As<IQueryable<Organization>>().Setup(m => m.Expression).Returns(MockDataContext.organizationDb.Expression);
                mockOrganization.As<IQueryable<Organization>>().Setup(m => m.ElementType).Returns(MockDataContext.organizationDb.ElementType);
                mockOrganization.As<IQueryable<Organization>>().Setup(m => m.GetEnumerator()).Returns(MockDataContext.organizationDb.GetEnumerator());
                
                var mockOrganizationElementRef = new Mock<DbSet<OrganizationRoleElementRef>>();
                mockOrganizationElementRef.As<IQueryable<OrganizationRoleElementRef>>().Setup(m => m.Provider).Returns(MockDataContext.organizationElementRefDb.Provider);
                mockOrganizationElementRef.As<IQueryable<OrganizationRoleElementRef>>().Setup(m => m.Expression).Returns(MockDataContext.organizationElementRefDb.Expression);
                mockOrganizationElementRef.As<IQueryable<OrganizationRoleElementRef>>().Setup(m => m.ElementType).Returns(MockDataContext.organizationElementRefDb.ElementType);
                mockOrganizationElementRef.As<IQueryable<OrganizationRoleElementRef>>().Setup(m => m.GetEnumerator()).Returns(MockDataContext.organizationElementRefDb.GetEnumerator());
                
                var mockOrganizationElementSetRef = new Mock<DbSet<OrganizationRoleElementSetRef>>();
                mockOrganizationElementSetRef.As<IQueryable<OrganizationRoleElementSetRef>>().Setup(m => m.Provider).Returns(MockDataContext.organizationElementSetRefDb.Provider);
                mockOrganizationElementSetRef.As<IQueryable<OrganizationRoleElementSetRef>>().Setup(m => m.Expression).Returns(MockDataContext.organizationElementSetRefDb.Expression);
                mockOrganizationElementSetRef.As<IQueryable<OrganizationRoleElementSetRef>>().Setup(m => m.ElementType).Returns(MockDataContext.organizationElementSetRefDb.ElementType);
                mockOrganizationElementSetRef.As<IQueryable<OrganizationRoleElementSetRef>>().Setup(m => m.GetEnumerator()).Returns(MockDataContext.organizationElementSetRefDb.GetEnumerator());

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

                var mockRerence = new Mock<DbSet<Reference>>();
                mockRerence.As<IQueryable<Reference>>().Setup(m => m.Provider).Returns(MockDataContext.referenceDb.Provider);
                mockRerence.As<IQueryable<Reference>>().Setup(m => m.Expression).Returns(MockDataContext.referenceDb.Expression);
                mockRerence.As<IQueryable<Reference>>().Setup(m => m.ElementType).Returns(MockDataContext.referenceDb.ElementType);
                mockRerence.As<IQueryable<Reference>>().Setup(m => m.GetEnumerator()).Returns(MockDataContext.referenceDb.GetEnumerator());

                var mockRerenceRef = new Mock<DbSet<ReferenceRef>>();
                mockRerenceRef.As<IQueryable<ReferenceRef>>().Setup(m => m.Provider).Returns(MockDataContext.referenceRefDb.Provider);
                mockRerenceRef.As<IQueryable<ReferenceRef>>().Setup(m => m.Expression).Returns(MockDataContext.referenceRefDb.Expression);
                mockRerenceRef.As<IQueryable<ReferenceRef>>().Setup(m => m.ElementType).Returns(MockDataContext.referenceRefDb.ElementType);
                mockRerenceRef.As<IQueryable<ReferenceRef>>().Setup(m => m.GetEnumerator()).Returns(MockDataContext.referenceRefDb.GetEnumerator());

                var mockSpecialty = new Mock<DbSet<Specialty>>();
                mockSpecialty.As<IQueryable<Specialty>>().Setup(m => m.Provider).Returns(MockDataContext.specialtyDb.Provider);
                mockSpecialty.As<IQueryable<Specialty>>().Setup(m => m.Expression).Returns(MockDataContext.specialtyDb.Expression);
                mockSpecialty.As<IQueryable<Specialty>>().Setup(m => m.ElementType).Returns(MockDataContext.specialtyDb.ElementType);
                mockSpecialty.As<IQueryable<Specialty>>().Setup(m => m.GetEnumerator()).Returns(MockDataContext.specialtyDb.GetEnumerator());

                var mockSpecialtyElementSetRef = new Mock<DbSet<SpecialtyElementSetRef>>();
                mockSpecialtyElementSetRef.As<IQueryable<SpecialtyElementSetRef>>().Setup(m => m.Provider).Returns(MockDataContext.specialtyElementSetDb.Provider);
                mockSpecialtyElementSetRef.As<IQueryable<SpecialtyElementSetRef>>().Setup(m => m.Expression).Returns(MockDataContext.specialtyElementSetDb.Expression);
                mockSpecialtyElementSetRef.As<IQueryable<SpecialtyElementSetRef>>().Setup(m => m.ElementType).Returns(MockDataContext.specialtyElementSetDb.ElementType);
                mockSpecialtyElementSetRef.As<IQueryable<SpecialtyElementSetRef>>().Setup(m => m.GetEnumerator()).Returns(MockDataContext.specialtyElementSetDb.GetEnumerator());

                var mockSpecialtyElementRef = new Mock<DbSet<SpecialtyElementRef>>();
                mockSpecialtyElementRef.As<IQueryable<SpecialtyElementRef>>().Setup(m => m.Provider).Returns(MockDataContext.specialtyElementDb.Provider);
                mockSpecialtyElementRef.As<IQueryable<SpecialtyElementRef>>().Setup(m => m.Expression).Returns(MockDataContext.specialtyElementDb.Expression);
                mockSpecialtyElementRef.As<IQueryable<SpecialtyElementRef>>().Setup(m => m.ElementType).Returns(MockDataContext.specialtyElementDb.ElementType);
                mockSpecialtyElementRef.As<IQueryable<SpecialtyElementRef>>().Setup(m => m.GetEnumerator()).Returns(MockDataContext.specialtyElementDb.GetEnumerator());

                var mockImage = new Mock<DbSet<Image>>();
                mockImage.As<IQueryable<Image>>().Setup(m => m.Provider).Returns(MockDataContext.imagesDb.Provider);
                mockImage.As<IQueryable<Image>>().Setup(m => m.Expression).Returns(MockDataContext.imagesDb.Expression);
                mockImage.As<IQueryable<Image>>().Setup(m => m.ElementType).Returns(MockDataContext.imagesDb.ElementType);
                mockImage.As<IQueryable<Image>>().Setup(m => m.GetEnumerator()).Returns(MockDataContext.imagesDb.GetEnumerator());

                var mockImageRef = new Mock<DbSet<ImageRef>>();
                mockImageRef.As<IQueryable<ImageRef>>().Setup(m => m.Provider).Returns(MockDataContext.imageRefDb.Provider);
                mockImageRef.As<IQueryable<ImageRef>>().Setup(m => m.Expression).Returns(MockDataContext.imageRefDb.Expression);
                mockImageRef.As<IQueryable<ImageRef>>().Setup(m => m.ElementType).Returns(MockDataContext.imageRefDb.ElementType);
                mockImageRef.As<IQueryable<ImageRef>>().Setup(m => m.GetEnumerator()).Returns(MockDataContext.imageRefDb.GetEnumerator());
                mockRadElementContext.Setup(c => c.Element).Returns(mockElement.Object);
                mockRadElementContext.Setup(c => c.ElementSet).Returns(mockSet.Object);
                mockRadElementContext.Setup(c => c.ElementSetRef).Returns(mockElementSetRef.Object);
                mockRadElementContext.Setup(c => c.ElementValue).Returns(mockElementValue.Object);
                mockRadElementContext.Setup(c => c.IndexCodeSystem).Returns(mockIndexCodeSystem.Object);
                mockRadElementContext.Setup(c => c.IndexCode).Returns(mockIndexCode.Object);
                mockRadElementContext.Setup(c => c.IndexCodeElementRef).Returns(mockIndexCodeElementRef.Object);
                mockRadElementContext.Setup(c => c.IndexCodeElementSetRef).Returns(mockIndexCodeElementSetRef.Object);
                mockRadElementContext.Setup(c => c.IndexCodeElementValueRef).Returns(mockIndexCodeElementValueRef.Object);
                mockRadElementContext.Setup(c => c.Person).Returns(mockPerson.Object);
                mockRadElementContext.Setup(c => c.PersonRoleElementRef).Returns(mockPersonElementRef.Object);
                mockRadElementContext.Setup(c => c.PersonRoleElementSetRef).Returns(mockPersonElementSetRef.Object);
                mockRadElementContext.Setup(c => c.Organization).Returns(mockOrganization.Object);
                mockRadElementContext.Setup(c => c.OrganizationRoleElementRef).Returns(mockOrganizationElementRef.Object);
                mockRadElementContext.Setup(c => c.OrganizationRoleElementSetRef).Returns(mockOrganizationElementSetRef.Object);
                mockRadElementContext.Setup(c => c.Reference).Returns(mockRerence.Object);
                mockRadElementContext.Setup(c => c.ReferenceRef).Returns(mockRerenceRef.Object);
                mockRadElementContext.Setup(c => c.Specialty).Returns(mockSpecialty.Object);
                mockRadElementContext.Setup(c => c.SpecialtyElementSetRef).Returns(mockSpecialtyElementSetRef.Object);
                mockRadElementContext.Setup(c => c.SpecialtyElementRef).Returns(mockSpecialtyElementRef.Object);
                mockRadElementContext.Setup(c => c.Image).Returns(mockImage.Object);
                mockRadElementContext.Setup(c => c.ImageRef).Returns(mockImageRef.Object);
            }

            var mockConfigurationManager = new Mock<IConfigurationManager>();
            var options = new DbContextOptionsBuilder<RadElementDbContext>().UseMySql(connectionString).Options;

            mockRadElementContext.Setup(c => c.Database).Returns(new DatabaseFacade(new RadElementDbContext(options, mockConfigurationManager.Object)));
        }

        #endregion
    }
}
