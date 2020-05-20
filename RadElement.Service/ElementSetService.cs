﻿using RadElement.Core.Domain;
using RadElement.Core.DTO;
using RadElement.Core.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using RadElement.Core.Data;

namespace RadElement.Service
{
    /// <summary>
    /// Business service for handling the element set related operations
    /// </summary>
    /// <seealso cref="RadElement.Core.Services.IElementSetService" />
    public class ElementSetService : IElementSetService
    {
        /// <summary>
        /// The RAD element database context
        /// </summary>
        private RadElementDbContext radElementDbContext;

        /// <summary>
        /// The mapper
        /// </summary>
        private readonly IMapper mapper;

        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ElementSetService" /> class.
        /// </summary>
        /// <param name="radElementDbContext">The RAD element database context.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="logger">The logger.</param>
        public ElementSetService(
            RadElementDbContext radElementDbContext,
            IMapper mapper,
            ILogger logger)
        {
            this.radElementDbContext = radElementDbContext;
            this.mapper = mapper;
            this.logger = logger;
        }

        /// <summary>
        /// Gets the set.
        /// </summary>
        /// <returns></returns>
        public async Task<JsonResult> GetSets()
        {
            try
            {
                var sets = (from elementSet in radElementDbContext.ElementSet
                            join setIndexCodeRef in radElementDbContext.IndexCodeElementSetRef on elementSet.Id equals setIndexCodeRef.ElementSetId into indCodeRef
                            from indexCodeRef in indCodeRef.DefaultIfEmpty()
                            join setIndexCode in radElementDbContext.IndexCode on indexCodeRef.CodeId equals setIndexCode.Id into indCode
                            from indexCode in indCode.DefaultIfEmpty()
                            join setPersonRef in radElementDbContext.PersonRoleElementSetRef on elementSet.Id equals setPersonRef.ElementSetID into perRef
                            from personRef in perRef.DefaultIfEmpty()
                            join setPerson in radElementDbContext.Person on personRef.PersonID equals setPerson.Id into pers
                            from person in pers.DefaultIfEmpty()
                            join setOrganizationRef in radElementDbContext.OrganizationRoleElementSetRef on elementSet.Id equals setOrganizationRef.ElementSetID into orgRef
                            from organizaionRef in orgRef.DefaultIfEmpty()
                            join setOrganization in radElementDbContext.Organization on organizaionRef.OrganizationID equals setOrganization.Id into org
                            from organization in org.DefaultIfEmpty()
                            select new FilteredData
                            {
                                ElementSet = elementSet,
                                IndexCode = indexCode,
                                Person = person == null ? null : new PersonAttributes
                                {
                                    Id = person.Id,
                                    Name = person.Name,
                                    Orcid = person.Orcid,
                                    TwitterHandle = person.TwitterHandle,
                                    Url = person.Url,
                                    Roles = !string.IsNullOrEmpty(personRef.Role) ? new List<string> { personRef.Role } : new List<string>()
                                },
                                Organization = organization == null ? null : new OrganizationAttributes
                                {
                                    Id = organization.Id,
                                    Name = organization.Name,
                                    Abbreviation = organization.Abbreviation,
                                    TwitterHandle = organization.TwitterHandle,
                                    Comment = organization.Comment,
                                    Email = organization.Email,
                                    Url = organization.Url,
                                    Roles = !string.IsNullOrEmpty(organizaionRef.Role) ? new List<string> { organizaionRef.Role } : new List<string>()
                                }
                            }).Distinct().ToList();

                return await Task.FromResult(new JsonResult(GetElementSetDetailsDto(sets, false), HttpStatusCode.OK));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception in method 'GetSets()'");
                var exMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return await Task.FromResult(new JsonResult(exMessage, HttpStatusCode.InternalServerError));
            }
        }

        /// <summary>
        /// Gets the set.
        /// </summary>
        /// <param name="setId">The set identifier.</param>
        /// <returns></returns>
        public async Task<JsonResult> GetSet(string setId)
        {
            try
            {
                if (IsValidSetId(setId))
                {
                    int id = Convert.ToInt32(setId.Remove(0, 4));
                    var selectedSets = (from elementSet in radElementDbContext.ElementSet
                                        join setIndexCodeRef in radElementDbContext.IndexCodeElementSetRef on elementSet.Id equals setIndexCodeRef.ElementSetId into indCodeRef
                                        from indexCodeRef in indCodeRef.DefaultIfEmpty()
                                        join setIndexCode in radElementDbContext.IndexCode on indexCodeRef.CodeId equals setIndexCode.Id into indCode
                                        from indexCode in indCode.DefaultIfEmpty()
                                        join setPersonRef in radElementDbContext.PersonRoleElementSetRef on elementSet.Id equals setPersonRef.ElementSetID into perRef
                                        from personRef in perRef.DefaultIfEmpty()
                                        join setPerson in radElementDbContext.Person on personRef.PersonID equals setPerson.Id into pers
                                        from person in pers.DefaultIfEmpty()
                                        join setOrganizationRef in radElementDbContext.OrganizationRoleElementSetRef on elementSet.Id equals setOrganizationRef.ElementSetID into orgRef
                                        from organizaionRef in orgRef.DefaultIfEmpty()
                                        join setOrganization in radElementDbContext.Organization on organizaionRef.OrganizationID equals setOrganization.Id into org
                                        from organization in org.DefaultIfEmpty()
                                        where elementSet.Id == id
                                        select new FilteredData
                                        {
                                            ElementSet = elementSet,
                                            IndexCode = indexCode,
                                            Person = person == null ? null : new PersonAttributes
                                            {
                                                Id = person.Id,
                                                Name = person.Name,
                                                Orcid = person.Orcid,
                                                TwitterHandle = person.TwitterHandle,
                                                Url = person.Url,
                                                Roles = !string.IsNullOrEmpty(personRef.Role) ? new List<string> { personRef.Role } : new List<string>()
                                            },
                                            Organization = organization == null ? null : new OrganizationAttributes
                                            {
                                                Id = organization.Id,
                                                Name = organization.Name,
                                                Abbreviation = organization.Abbreviation,
                                                TwitterHandle = organization.TwitterHandle,
                                                Comment = organization.Comment,
                                                Email = organization.Email,
                                                Url = organization.Url,
                                                Roles = !string.IsNullOrEmpty(organizaionRef.Role) ? new List<string> { organizaionRef.Role } : new List<string>()
                                            }
                                        }).Distinct().ToList();

                    if (selectedSets != null && selectedSets.Any())
                    {
                        return await Task.FromResult(new JsonResult(GetElementSetDetailsDto(selectedSets, true), HttpStatusCode.OK));
                    }
                }

                return await Task.FromResult(new JsonResult(string.Format("No such set with id '{0}'.", setId), HttpStatusCode.NotFound));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception in method 'GetSet(string setId)'");
                var exMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return await Task.FromResult(new JsonResult(exMessage, HttpStatusCode.InternalServerError));
            }
        }

        /// <summary>
        /// Searches the cde set.
        /// </summary>
        /// <param name="searchKeyword">The search keyword.</param>
        /// <returns></returns>
        public async Task<JsonResult> SearchSets(string searchKeyword)
        {
            try
            {
                if (!string.IsNullOrEmpty(searchKeyword))
                {
                    var filteredSets = (from elementSet in radElementDbContext.ElementSet
                                        join setIndexCodeRef in radElementDbContext.IndexCodeElementSetRef on elementSet.Id equals setIndexCodeRef.ElementSetId into indCodeRef
                                        from indexCodeRef in indCodeRef.DefaultIfEmpty()
                                        join setIndexCode in radElementDbContext.IndexCode on indexCodeRef.CodeId equals setIndexCode.Id into indCode
                                        from indexCode in indCode.DefaultIfEmpty()
                                        join setPersonRef in radElementDbContext.PersonRoleElementSetRef on elementSet.Id equals setPersonRef.ElementSetID into perRef
                                        from personRef in perRef.DefaultIfEmpty()
                                        join setPerson in radElementDbContext.Person on personRef.PersonID equals setPerson.Id into pers
                                        from person in pers.DefaultIfEmpty()
                                        join setOrganizationRef in radElementDbContext.OrganizationRoleElementSetRef on elementSet.Id equals setOrganizationRef.ElementSetID into orgRef
                                        from organizaionRef in orgRef.DefaultIfEmpty()
                                        join setOrganization in radElementDbContext.Organization on organizaionRef.OrganizationID equals setOrganization.Id into org
                                        from organization in org.DefaultIfEmpty()
                                        where (elementSet.Name.Contains(searchKeyword, StringComparison.InvariantCultureIgnoreCase) ||
                                               ("RDES" + elementSet.Id.ToString()).Contains(searchKeyword, StringComparison.InvariantCultureIgnoreCase))
                                        select new FilteredData
                                        {
                                            ElementSet = elementSet,
                                            IndexCode = indexCode,
                                            Person = person == null ? null : new PersonAttributes
                                            {
                                                Id = person.Id,
                                                Name = person.Name,
                                                Orcid = person.Orcid,
                                                TwitterHandle = person.TwitterHandle,
                                                Url = person.Url,
                                                Roles = !string.IsNullOrEmpty(personRef.Role) ? new List<string> { personRef.Role } : new List<string>()
                                            },
                                            Organization = organization == null ? null : new OrganizationAttributes
                                            {
                                                Id = organization.Id,
                                                Name = organization.Name,
                                                Abbreviation = organization.Abbreviation,
                                                TwitterHandle = organization.TwitterHandle,
                                                Comment = organization.Comment,
                                                Email = organization.Email,
                                                Url = organization.Url,
                                                Roles = !string.IsNullOrEmpty(organizaionRef.Role) ? new List<string> { organizaionRef.Role } : new List<string>()
                                            }
                                        }).Distinct().ToList();

                    if (filteredSets != null && filteredSets.Any())
                    {
                        return await Task.FromResult(new JsonResult(GetElementSetDetailsDto(filteredSets, false), HttpStatusCode.OK));
                    }
                    else
                    {
                        return await Task.FromResult(new JsonResult(string.Format("No such set with keyword '{0}'.", searchKeyword), HttpStatusCode.NotFound));
                    }
                }

                return await Task.FromResult(new JsonResult("Keyword given is invalid.", HttpStatusCode.BadRequest));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception in method 'SearchSets(string searchKeyword)'");
                var exMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return await Task.FromResult(new JsonResult(exMessage, HttpStatusCode.InternalServerError));
            }
        }

        /// <summary>
        /// Creates the cde set.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <returns></returns>
        public async Task<JsonResult> CreateSet(CreateUpdateSet content)
        {
            using (var transaction = radElementDbContext.Database.BeginTransaction())
            {
                try
                {
                    if (content == null)
                    {
                        return await Task.FromResult(new JsonResult("Set fileds are invalid.", HttpStatusCode.BadRequest));
                    }

                    ElementSet set = new ElementSet()
                    {
                        Name = content.Name.Trim(),
                        Description = content.Description,
                        ContactName = content.ContactName,
                        ParentId = content.ParentId,
                        Modality = content.Modality != null && content.Modality.Any() ? string.Join(",", content.Modality) : null,
                        BiologicalSex = content.BiologicalSex != null && content.BiologicalSex.Any() ? string.Join(",", content.BiologicalSex) : null,
                        AgeLowerBound = content.AgeLowerBound,
                        AgeUpperBound = content.AgeUpperBound,
                        Status = "Proposed",
                        StatusDate = DateTime.UtcNow,
                        Version = content.Version,
                        VersionDate = content.VersionDate != null ? content.VersionDate : DateTime.Now,
                        Deleted_At = content.DeletedAt ?? DateTime.Now
                    };

                    radElementDbContext.ElementSet.Add(set);
                    radElementDbContext.SaveChanges();

                    AddIndexCodeReferences(set.Id, content.IndexCodeReferences);
                    AddPersonReferences(set.Id, content.Persons);
                    AddOrganizationReferences(set.Id, content.Organizations);

                    transaction.Commit();

                    return await Task.FromResult(new JsonResult(new SetIdDetails() { SetId = "RDES" + set.Id.ToString() }, HttpStatusCode.Created));
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    logger.Error(ex, "Exception in method 'CreateSet(CreateUpdateSet content)'");
                    var exMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    return await Task.FromResult(new JsonResult(exMessage, HttpStatusCode.InternalServerError));
                }
            }
        }

        /// <summary>
        /// Updates the set.
        /// </summary>
        /// <param name="setId">The set identifier.</param>
        /// <param name="content">The content.</param>
        /// <returns></returns>
        public async Task<JsonResult> UpdateSet(string setId, CreateUpdateSet content)
        {
            using (var transaction = radElementDbContext.Database.BeginTransaction())
            {
                try
                {
                    if (IsValidSetId(setId))
                    {
                        int id = Convert.ToInt32(setId.Remove(0, 4));

                        if (content == null)
                        {
                            return await Task.FromResult(new JsonResult("Set fileds are invalid.", HttpStatusCode.BadRequest));
                        }

                        var elementSet = radElementDbContext.ElementSet.Where(x => x.Id == id).FirstOrDefault();

                        if (elementSet != null)
                        {
                            elementSet.Name = content.Name.Trim();
                            elementSet.Description = content.Description;
                            elementSet.ContactName = content.ContactName;
                            elementSet.ParentId = content.ParentId;
                            elementSet.Modality = content.Modality != null && content.Modality.Any() ? string.Join(",", content.Modality) : null;
                            elementSet.BiologicalSex = content.BiologicalSex != null && content.BiologicalSex.Any() ? string.Join(",", content.BiologicalSex) : null;
                            elementSet.AgeLowerBound = content.AgeLowerBound;
                            elementSet.AgeUpperBound = content.AgeUpperBound;
                            elementSet.Version = content.Version;
                            elementSet.VersionDate = content.VersionDate != null ? content.VersionDate : DateTime.Now;
                            elementSet.Deleted_At = content.DeletedAt ?? DateTime.Now;

                            radElementDbContext.SaveChanges();

                            RemoveIndexCodeReferences(id);
                            RemovePersonReferences(id);
                            RemoveOrganizationReferences(id);

                            AddIndexCodeReferences(id, content.IndexCodeReferences);
                            AddPersonReferences(id, content.Persons);
                            AddOrganizationReferences(id, content.Organizations);

                            transaction.Commit();

                            return await Task.FromResult(new JsonResult(string.Format("Set with id '{0}' is updated.", setId), HttpStatusCode.OK));
                        }
                    }

                    return await Task.FromResult(new JsonResult(string.Format("No such set with id '{0}'.", setId), HttpStatusCode.NotFound));
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    logger.Error(ex, "Exception in method 'UpdateSet(string setId, CreateUpdateSet content)'");
                    var exMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    return await Task.FromResult(new JsonResult(exMessage, HttpStatusCode.InternalServerError));
                }
            }
        }

        /// <summary>
        /// Deletes the set.
        /// </summary>
        /// <param name="setId">The set identifier.</param>
        /// <returns></returns>
        public async Task<JsonResult> DeleteSet(string setId)
        {
            using (var transaction = radElementDbContext.Database.BeginTransaction())
            {
                try
                {
                    if (IsValidSetId(setId))
                    {
                        int id = Convert.ToInt32(setId.Remove(0, 4));
                        var elementSet = radElementDbContext.ElementSet.Where(x => x.Id == id).FirstOrDefault();

                        if (elementSet != null)
                        {
                            RemoveIndexCodeReferences(elementSet.Id);
                            RemoveSetElementsReferences(elementSet);
                            RemovePersonReferences(elementSet.Id);
                            RemoveOrganizationReferences(elementSet.Id);
                            RemoveSet(elementSet.Id);

                            transaction.Commit();

                            return await Task.FromResult(new JsonResult(string.Format("Set with id '{0}' is deleted.", setId), HttpStatusCode.OK));
                        }
                    }
                    return await Task.FromResult(new JsonResult(string.Format("No such set with id '{0}'.", setId), HttpStatusCode.NotFound));
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    logger.Error(ex, "Exception in method 'DeleteSet(string setId)'");
                    var exMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    return await Task.FromResult(new JsonResult(exMessage, HttpStatusCode.InternalServerError));
                }
            }
        }

        /// <summary>
        /// Removes the set elements.
        /// </summary>
        /// <param name="elementSet">The element set.</param>
        private void RemoveSetElementsReferences(ElementSet elementSet)
        {
            var elementSetRefs = radElementDbContext.ElementSetRef.Where(x => x.ElementSetId == elementSet.Id).ToList();
            if (elementSetRefs != null && elementSetRefs.Any())
            {
                radElementDbContext.ElementSetRef.RemoveRange(elementSetRefs);
                radElementDbContext.SaveChanges();
            }
        }

        /// <summary>
        /// Adds the person references.
        /// </summary>
        /// <param name="personIds">The person ids.</param>
        private void AddPersonReferences(int setId, List<PersonDetails> personRefs)
        {
            if (personRefs != null && personRefs.Any())
            {
                foreach (var personRef in personRefs)
                {
                    var person = radElementDbContext.Person.Where(x => x.Id == personRef.PersonId).FirstOrDefault();

                    if (person != null)
                    {
                        if (personRef.Roles != null && personRef.Roles.Any())
                        {
                            foreach (var role in personRef.Roles.Distinct())
                            {
                                var setRef = new PersonRoleElementSetRef()
                                {
                                    ElementSetID = setId,
                                    PersonID = personRef.PersonId,
                                    Role = role.ToString()
                                };

                                radElementDbContext.PersonRoleElementSetRef.Add(setRef);
                                radElementDbContext.SaveChanges();
                            }
                        }
                        else
                        {
                            var setRef = new PersonRoleElementSetRef()
                            {
                                ElementSetID = setId,
                                PersonID = personRef.PersonId
                            };

                            radElementDbContext.PersonRoleElementSetRef.Add(setRef);
                            radElementDbContext.SaveChanges();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds the organization references.
        /// </summary>
        /// <param name="setId">The set identifier.</param>
        /// <param name="orgRefs">The org refs.</param>
        private void AddOrganizationReferences(int setId, List<OrganizationDetails> orgRefs)
        {
            if (orgRefs != null && orgRefs.Any())
            {
                foreach (var orgRef in orgRefs)
                {
                    var organization = radElementDbContext.Organization.Where(x => x.Id == orgRef.OrganizationId).FirstOrDefault();

                    if (organization != null)
                    {
                        if (orgRef.Roles != null && orgRef.Roles.Any())
                        {
                            foreach (var role in orgRef.Roles.Distinct())
                            {
                                var setRef = new OrganizationRoleElementSetRef()
                                {
                                    ElementSetID = setId,
                                    OrganizationID = orgRef.OrganizationId,
                                    Role = role.ToString()
                                };

                                radElementDbContext.OrganizationRoleElementSetRef.Add(setRef);
                                radElementDbContext.SaveChanges();
                            }
                        }
                        else
                        {
                            var setRef = new OrganizationRoleElementSetRef()
                            {
                                ElementSetID = setId,
                                OrganizationID = orgRef.OrganizationId
                            };

                            radElementDbContext.OrganizationRoleElementSetRef.Add(setRef);
                            radElementDbContext.SaveChanges();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds the index code references.
        /// </summary>
        /// <param name="setId">The set identifier.</param>
        /// <param name="codeReference">The code reference.</param>
        private void AddIndexCodeReferences(int setId, List<IndexCodeReference> codeReferences)
        {
            if (codeReferences != null && codeReferences.Any())
            {
                foreach (var codeReference in codeReferences)
                {
                    int codeId = 0;
                    var indexCodeSystem = radElementDbContext.IndexCodeSystem.Where(x => string.Equals(x.Abbrev, codeReference.System, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                    if (indexCodeSystem != null)
                    {
                        var indexCode = radElementDbContext.IndexCode.Where(x => string.Equals(x.System, indexCodeSystem.Abbrev, StringComparison.InvariantCultureIgnoreCase) &&
                                                                                 string.Equals(x.Code, codeReference.Code, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                        if (indexCode != null)
                        {
                            codeId = indexCode.Id;
                        }
                        else
                        {
                            var indexCodeSys = new IndexCode
                            {
                                Code = codeReference.Code,
                                System = indexCodeSystem.Abbrev,
                                Display = codeReference.Display,
                                AccessionDate = DateTime.UtcNow
                            };
                            radElementDbContext.IndexCode.Add(indexCodeSys);
                            radElementDbContext.SaveChanges();

                            codeId = indexCodeSys.Id;
                        }
                        var setIndexCode = new IndexCodeElementSetRef
                        {
                            ElementSetId = setId,
                            CodeId = codeId
                        };

                        radElementDbContext.IndexCodeElementSetRef.Add(setIndexCode);
                        radElementDbContext.SaveChanges();
                    }
                }
            }
        }

        /// <summary>
        /// Removes the set.
        /// </summary>
        /// <param name="setId">The set identifier.</param>
        private void RemoveSet(int setId)
        {
            var set = radElementDbContext.ElementSet.Where(x => x.Id == setId).FirstOrDefault();
            if (set != null)
            {
                radElementDbContext.ElementSet.Remove(set);
                radElementDbContext.SaveChanges();
            }
        }

        /// <summary>
        /// Removes the person references.
        /// </summary>
        /// <param name="setId">The set identifier.</param>
        private void RemovePersonReferences(int setId)
        {
            var personElementSetRefs = radElementDbContext.PersonRoleElementSetRef.Where(x => x.ElementSetID == setId).ToList();
            if (personElementSetRefs != null && personElementSetRefs.Any())
            {
                radElementDbContext.PersonRoleElementSetRef.RemoveRange(personElementSetRefs);
                radElementDbContext.SaveChanges();
            }
        }

        /// <summary>
        /// Removes the organization references.
        /// </summary>
        /// <param name="setId">The set identifier.</param>
        private void RemoveOrganizationReferences(int setId)
        {
            var organizationElementSetRefs = radElementDbContext.OrganizationRoleElementSetRef.Where(x => x.ElementSetID == setId).ToList();
            if (organizationElementSetRefs != null && organizationElementSetRefs.Any())
            {
                radElementDbContext.OrganizationRoleElementSetRef.RemoveRange(organizationElementSetRefs);
                radElementDbContext.SaveChanges();
            }
        }
        /// <summary>
        /// Removes the index code references.
        /// </summary>
        /// <param name="setId">The set identifier.</param>
        private void  RemoveIndexCodeReferences(int setId)
        {
            var indexCodeSetRefs = radElementDbContext.IndexCodeElementSetRef.Where(x => x.ElementSetId == setId).ToList();
            if (indexCodeSetRefs != null && indexCodeSetRefs.Any())
            {
                radElementDbContext.IndexCodeElementSetRef.RemoveRange(indexCodeSetRefs);
                radElementDbContext.SaveChanges();
            }
        }

        /// <summary>
        /// Determines whether [is valid set identifier] [the specified set identifier].
        /// </summary>
        /// <param name="setId">The set identifier.</param>
        /// <returns>
        ///   <c>true</c> if [is valid set identifier] [the specified set identifier]; otherwise, <c>false</c>.
        /// </returns>
        private bool IsValidSetId(string setId)
        {
            if (setId.Length > 4 && string.Equals(setId.Substring(0, 4), "RDES", StringComparison.InvariantCultureIgnoreCase))
            {
                bool result = int.TryParse(setId.Remove(0, 4), out _);
                return result;
            }

            return false;
        }

        /// <summary>
        /// Gets the element set details dto.
        /// </summary>
        /// <param name="filteredSets">The filtered sets.</param>
        /// <param name="isSingleSet">if set to <c>true</c> [is single set].</param>
        /// <returns></returns>
        private object GetElementSetDetailsDto(List<FilteredData> filteredSets, bool isSingleSet)
        {
            var sets = new List<ElementSetDetails>();
            foreach (var eleSet in filteredSets)
            {
                if (!sets.Exists(x => x.Id == eleSet.ElementSet.Id))
                {
                    var set = mapper.Map<ElementSetDetails>(eleSet.ElementSet);
                    if (eleSet.Person != null)
                    {
                        set.PersonInformation = new List<PersonAttributes>();
                        set.PersonInformation.Add(eleSet.Person);
                    }
                    if (eleSet.Organization != null)
                    {
                        set.OrganizationInformation = new List<OrganizationAttributes>();
                        set.OrganizationInformation.Add(eleSet.Organization);
                    }
                    if (eleSet.IndexCode != null)
                    {
                        set.IndexCodes = new List<IndexCode>();
                        set.IndexCodes.Add(eleSet.IndexCode);
                    }

                    sets.Add(set);
                }
                else
                {
                    var set = sets.Find(x => x.Id == eleSet.ElementSet.Id);
                    if (eleSet.IndexCode != null)
                    {
                        if (!set.IndexCodes.Exists(x => x.Id == eleSet.IndexCode.Id))
                        {
                            if (set.IndexCodes == null)
                            {
                                set.IndexCodes = new List<IndexCode>();
                            }
                            set.IndexCodes.Add(eleSet.IndexCode);
                        }
                    }
                    if (eleSet.Person != null)
                    {
                        if (!set.PersonInformation.Exists(x => x.Id == eleSet.Person.Id))
                        {
                            if (set.PersonInformation == null)
                            {
                                set.PersonInformation = new List<PersonAttributes>();
                            }
                            set.PersonInformation.Add(eleSet.Person);
                        }
                        else
                        {
                            var person = set.PersonInformation.Find(x => x.Id == eleSet.Person.Id);
                            if (person != null)
                            {
                                if (eleSet.Person.Roles.Any() && !person.Roles.Exists(x => x == eleSet.Person.Roles[0]))
                                {
                                    person.Roles.Add(eleSet.Person.Roles[0]);
                                }
                            }
                        }
                    }
                    if (eleSet.Organization != null)
                    {
                        if (!set.OrganizationInformation.Exists(x => x.Id == eleSet.Organization.Id))
                        {
                            if (set.OrganizationInformation == null)
                            {
                                set.OrganizationInformation = new List<OrganizationAttributes>();
                            }
                            set.OrganizationInformation.Add(eleSet.Organization);
                        }
                        else
                        {
                            var organization = set.OrganizationInformation.Find(x => x.Id == eleSet.Organization.Id);
                            if (organization != null)
                            {
                                if (eleSet.Organization.Roles.Any() && !organization.Roles.Exists(x => x == eleSet.Organization.Roles[0]))
                                {
                                    organization.Roles.Add(eleSet.Organization.Roles[0]);
                                }
                            }
                        }
                    }
                }
            }
            if (isSingleSet)
            {
                return sets.FirstOrDefault();
            }

            return sets;
        }
    }
}
