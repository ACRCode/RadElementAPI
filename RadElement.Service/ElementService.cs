﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RadElement.Core.Domain;
using RadElement.Core.DTO;
using RadElement.Core.Services;
using System.Net;
using Serilog;
using AutoMapper;
using RadElement.Core.Data;

namespace RadElement.Service
{
    /// <summary>
    /// Business service for handling the element related operations
    /// </summary>
    /// <seealso cref="RadElement.Core.Services.IElementService" />
    public class ElementService : IElementService
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
        /// Initializes a new instance of the <see cref="ElementService" /> class.
        /// </summary>
        /// <param name="radElementDbContext">The RAD element database context.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="logger">The logger.</param>
        public ElementService(
            RadElementDbContext radElementDbContext,
            IMapper mapper,
            ILogger logger)
        {
            this.radElementDbContext = radElementDbContext;
            this.mapper = mapper;
            this.logger = logger;
        }

        /// <summary>
        /// Gets the element.
        /// </summary>
        /// <returns></returns>
        public async Task<JsonResult> GetElements()
        {
            try
            {
                var elements = radElementDbContext.Element.ToList();
                return await Task.FromResult(new JsonResult(GetElementDetailsDto(elements), HttpStatusCode.OK));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception in method 'GetElements()'");
                var exMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return await Task.FromResult(new JsonResult(exMessage, HttpStatusCode.InternalServerError));
            }
        }

        /// <summary>
        /// Gets the element.
        /// </summary>
        /// <param name="elementId">The element identifier.</param>
        /// <returns></returns>
        public async Task<JsonResult> GetElement(string elementId)
        {
            try
            {
                if (IsValidElementId(elementId))
                {
                    int elementInternalId = Convert.ToInt32(elementId.Remove(0, 3));
                    var elements = radElementDbContext.Element.ToList();
                    var element = elements.Find(x => x.Id == elementInternalId);

                    if (element != null)
                    {
                        return await Task.FromResult(new JsonResult(GetElementDetailsDto(element), HttpStatusCode.OK));
                    }
                }

                return await Task.FromResult(new JsonResult(string.Format("No such element with id '{0}'.", elementId), HttpStatusCode.NotFound));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception in method 'GetElement(string elementId)'");
                var exMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return await Task.FromResult(new JsonResult(exMessage, HttpStatusCode.InternalServerError));
            }
        }

        /// <summary>
        /// Gets the elements by set identifier.
        /// </summary>
        /// <param name="setId">The set identifier.</param>
        /// <returns></returns>
        public async Task<JsonResult> GetElementsBySetId(string setId)
        {
            try
            {
                if (IsValidSetId(setId))
                {
                    int setInternalId = Convert.ToInt32(setId.Remove(0, 4));
                    var setRefs = radElementDbContext.ElementSetRef.ToList();
                    var elementIds = setRefs.Where(x => x.ElementSetId == setInternalId);
                    var elements = radElementDbContext.Element.ToList();

                    var selectedElements = from elemetId in elementIds
                                           join element in elements on elemetId.ElementId equals (int)element.Id
                                           select element;

                    if (selectedElements != null && selectedElements.Any())
                    {
                        return await Task.FromResult(new JsonResult(GetElementDetailsDto(selectedElements.ToList()), HttpStatusCode.OK));
                    }
                }
                return await Task.FromResult(new JsonResult(string.Format("No such elements with set id '{0}'.", setId), HttpStatusCode.NotFound));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception in method 'GetElementsBySetId(string setId)'");
                var exMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return await Task.FromResult(new JsonResult(exMessage, HttpStatusCode.InternalServerError));
            }
        }

        /// <summary>
        /// Searches the element.
        /// </summary>
        /// <param name="searchKeyword">The search keyword.</param>
        /// <returns></returns>
        public async Task<JsonResult> SearchElements(string searchKeyword)
        {
            try
            {
                if (!string.IsNullOrEmpty(searchKeyword))
                {
                    if (searchKeyword.Length < 3)
                    {
                        return await Task.FromResult(new JsonResult("The Keyword field must be a string with a minimum length of '3'.", HttpStatusCode.BadRequest));
                    }

                    var elements = radElementDbContext.Element.ToList();
                    var filteredElements = elements.Where(x => string.Concat("RDE", x.Id).ToLower().Contains(searchKeyword.ToLower()) ||
                                                               x.Name.ToLower().Contains(searchKeyword.ToLower())).ToList();
                    if (filteredElements != null && filteredElements.Any())
                    {
                        return await Task.FromResult(new JsonResult(GetElementDetailsDto(filteredElements), HttpStatusCode.OK));
                    }
                    else
                    {
                        return await Task.FromResult(new JsonResult(string.Format("No such element with keyword '{0}'.", searchKeyword), HttpStatusCode.NotFound));
                    }
                }

                return await Task.FromResult(new JsonResult("Keyword given is invalid.", HttpStatusCode.BadRequest));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception in method 'SearchElements(SearchKeyword searchKeyword)'");
                var exMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return await Task.FromResult(new JsonResult(exMessage, HttpStatusCode.InternalServerError));
            }
        }

        /// <summary>
        /// Creates the element.
        /// </summary>
        /// <param name="setId">The set identifier.</param>
        /// <param name="content">The content.</param>
        /// <returns></returns>
        public async Task<JsonResult> CreateElement(string setId, DataElementType elementType, CreateUpdateElementOld dataElement)
        {
            using (var transaction = radElementDbContext.Database.BeginTransaction())
            {
                try
                {
                    if (IsValidSetId(setId))
                    {
                        int id = Convert.ToInt32(setId.Remove(0, 4));
                        if (dataElement == null)
                        {
                            return await Task.FromResult(new JsonResult("Element fields are invalid.", HttpStatusCode.BadRequest));
                        }

                        if (string.IsNullOrEmpty(dataElement.Label))
                        {
                            return await Task.FromResult(new JsonResult("'Label' field is missing in request", HttpStatusCode.BadRequest));
                        }

                        if (elementType == DataElementType.Choice || elementType == DataElementType.MultiChoice)
                        {
                            if (dataElement.Options == null)
                            {
                                return await Task.FromResult(new JsonResult("'Options' field is missing for Choice type elements in request", HttpStatusCode.BadRequest));
                            }
                        }

                        int elementId = 0;
                        var elementSets = radElementDbContext.ElementSet.ToList();
                        var elementSet = elementSets.Find(x => x.Id == id);

                        if (elementSet != null)
                        {
                            Element element = new Element()
                            {
                                Name = dataElement.Label,
                                ShortName = "",
                                Definition = dataElement.Definition ?? "",
                                MaxCardinality = 1,
                                MinCardinality = 1,
                                Source = "DSI TOUCH-AI",
                                Status = "Proposed",
                                StatusDate = DateTime.Now,
                                Editor = "",
                                Instructions = "",
                                Question = dataElement.Label ?? "",
                                References = "",
                                Synonyms = "",
                                VersionDate = DateTime.Now,
                                Version = "1",
                                ValueSize = 0,
                                Unit = ""
                            };

                            if (elementType == DataElementType.Integer)
                            {
                                element.ValueType = "integer";
                                element.ValueMin = dataElement.ValueMin;
                                element.ValueMax = dataElement.ValueMax;
                                element.StepValue = 1;
                                element.Unit = dataElement.Unit ?? "";
                            }

                            if (elementType == DataElementType.Numeric)
                            {
                                float? minValue = null;
                                float? maxValue = null;

                                if (dataElement.ValueMin.HasValue)
                                {
                                    minValue = Convert.ToSingle(dataElement.ValueMin.Value);
                                }

                                if (dataElement.ValueMax.HasValue)
                                {
                                    maxValue = Convert.ToSingle(dataElement.ValueMax.Value);
                                }
                                element.ValueType = "float";
                                element.ValueMin = minValue;
                                element.ValueMax = maxValue;
                                element.StepValue = 0.1f;
                                element.Unit = dataElement.Unit ?? "";
                            }

                            if (elementType == DataElementType.Choice)
                            {
                                element.ValueType = "valueSet";
                            }

                            if (elementType == DataElementType.MultiChoice)
                            {
                                element.ValueType = "valueSet";
                                element.MaxCardinality = (uint)dataElement.Options.Count;
                            }

                            if (elementType == DataElementType.DateTime)
                            {
                                element.ValueType = "date";
                            }

                            if (elementType == DataElementType.String)
                            {
                                element.ValueType = "string";
                            }

                            radElementDbContext.Element.Add(element);
                            radElementDbContext.SaveChanges();

                            elementId = (int)element.Id;

                            if (elementType == DataElementType.MultiChoice || elementType == DataElementType.Choice)
                            {
                                foreach (OptionOld option in dataElement.Options)
                                {
                                    ElementValue elementvalue = new ElementValue()
                                    {
                                        Name = option.Label,
                                        Value = string.Empty,
                                        Definition = option.Label,
                                        ElementId = element.Id
                                    };

                                    radElementDbContext.ElementValue.Add(elementvalue);
                                }
                            }
                        }

                        AddElementSetReferences(id, (short)elementId);
                        radElementDbContext.SaveChanges();
                        transaction.Commit();

                        return await Task.FromResult(new JsonResult(new ElementIdDetails() { ElementId = "RDE" + elementId.ToString() }, HttpStatusCode.Created));
                    }

                    return await Task.FromResult(new JsonResult(string.Format("No such set with set id {0}.", setId), HttpStatusCode.NotFound));
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    logger.Error(ex, "Exception in method 'CreateElement(int setId, DataElementType elementType, CreateUpdateElement dataElement)'");
                    var exMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    return await Task.FromResult(new JsonResult(exMessage, HttpStatusCode.InternalServerError));
                }
            }
        }

        /// <summary>
        /// Creates the element.
        /// </summary>
        /// <param name="setId">The set identifier.</param>
        /// <param name="dataElement">The data element.</param>
        /// <returns></returns>
        public async Task<JsonResult> CreateElement(string setId, CreateElement dataElement)
        {
            using (var transaction = radElementDbContext.Database.BeginTransaction())
            {
                try
                {
                    if (IsValidSetId(setId))
                    {
                        int setInternalId = Convert.ToInt32(setId.Remove(0, 4));
                        if (dataElement == null)
                        {
                            return await Task.FromResult(new JsonResult("Element fields are invalid.", HttpStatusCode.BadRequest));
                        }

                        if (dataElement.ValueType == DataElementType.Choice || dataElement.ValueType == DataElementType.MultiChoice)
                        {
                            if (dataElement.Options == null || dataElement.Options.Count == 0)
                            {
                                return await Task.FromResult(new JsonResult("'Options' field are missing for Choice type elements.", HttpStatusCode.BadRequest));
                            }
                        }

                        var elementSets = radElementDbContext.ElementSet.ToList();
                        var elementSet = elementSets.Find(x => x.Id == setInternalId);

                        if (elementSet != null)
                        {
                            if (string.IsNullOrEmpty(dataElement.ElementId))
                            {
                                Element element = new Element()
                                {
                                    Name = dataElement.Name,
                                    ShortName = dataElement.ShortName ?? string.Empty,
                                    Definition = dataElement.Definition ?? string.Empty,
                                    ValueType = GetElementValueType(dataElement.ValueType),
                                    MinCardinality = 1,
                                    MaxCardinality = (dataElement.ValueType == DataElementType.MultiChoice) ? (uint)dataElement.Options.Count : 1,
                                    Unit = dataElement.Unit ?? string.Empty,
                                    Question = dataElement.Question ?? dataElement.Name,
                                    Instructions = dataElement.Instructions ?? string.Empty,
                                    References = dataElement.References ?? string.Empty,
                                    Version = dataElement.Version ?? "",
                                    VersionDate = dataElement.VersionDate ?? DateTime.Now,
                                    Synonyms = dataElement.Synonyms ?? string.Empty,
                                    Source = dataElement.Source ?? string.Empty,
                                    Status = "Proposed",
                                    StatusDate = DateTime.Now,
                                    Editor = dataElement.Editor ?? string.Empty,
                                    Modality = dataElement.Modality != null && dataElement.Modality.Any() ? string.Join(",", dataElement.Modality) : null,
                                    BiologicalSex = dataElement.BiologicalSex != null && dataElement.BiologicalSex.Any() ? string.Join(",", dataElement.BiologicalSex) : null,
                                    AgeLowerBound = dataElement.AgeLowerBound,
                                    AgeUpperBound = dataElement.AgeUpperBound,
                                    ValueSize = 0
                                };

                                if (dataElement.ValueType == DataElementType.Integer)
                                {
                                    element.ValueMin = dataElement.ValueMin;
                                    element.ValueMax = dataElement.ValueMax;
                                    element.StepValue = 1;
                                }

                                if (dataElement.ValueType == DataElementType.Numeric)
                                {
                                    float? minValue = null;
                                    float? maxValue = null;

                                    if (dataElement.ValueMin.HasValue)
                                    {
                                        minValue = Convert.ToSingle(dataElement.ValueMin.Value);
                                    }

                                    if (dataElement.ValueMax.HasValue)
                                    {
                                        maxValue = Convert.ToSingle(dataElement.ValueMax.Value);
                                    }

                                    element.ValueMin = minValue;
                                    element.ValueMax = maxValue;
                                    element.StepValue = 0.1f;
                                }

                                radElementDbContext.Element.Add(element);
                                radElementDbContext.SaveChanges();

                                if (dataElement.ValueType == DataElementType.MultiChoice || dataElement.ValueType == DataElementType.Choice)
                                {
                                    AddElementValues(dataElement.Options, element.Id);
                                }

                                AddElementSetReferences(setInternalId, (int)element.Id);
                                AddPersonReferences((int)element.Id, dataElement.Persons);
                                AddOrganizationReferences((int)element.Id, dataElement.Organizations);

                                radElementDbContext.SaveChanges();
                                transaction.Commit();

                                return await Task.FromResult(new JsonResult(new ElementIdDetails() { ElementId = "RDE" + element.Id.ToString() }, HttpStatusCode.Created));
                            }
                            else
                            {
                                if (IsValidElementId(dataElement.ElementId))
                                {
                                    int elementInternalId = Convert.ToInt32(dataElement.ElementId.Remove(0, 3));
                                    var elements = radElementDbContext.Element.ToList();
                                    var element = elements.Find(x => x.Id == elementInternalId);

                                    if (element != null)
                                    {
                                        var elementsetRefs = radElementDbContext.ElementSetRef.ToList();
                                        if (!elementsetRefs.Exists(x => x.ElementId == elementInternalId && x.ElementSetId == setInternalId))
                                        {
                                            AddElementSetReferences(setInternalId, elementInternalId);
                                            radElementDbContext.SaveChanges();
                                            transaction.Commit();
                                        }

                                        return await Task.FromResult(new JsonResult(new ElementIdDetails() { ElementId = dataElement.ElementId }, HttpStatusCode.Created));
                                    }
                                }

                                return await Task.FromResult(new JsonResult(string.Format("No such element with element id '{0}'.", dataElement.ElementId), HttpStatusCode.NotFound));
                            }
                        }
                    }

                    return await Task.FromResult(new JsonResult(string.Format("No such set with set id {0}.", setId), HttpStatusCode.NotFound));
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    logger.Error(ex, "Exception in method 'CreateElement(string setId, CreateUpdateElement dataElement)'");
                    var exMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    return await Task.FromResult(new JsonResult(exMessage, HttpStatusCode.InternalServerError));
                }
            }
        }

        /// <summary>
        /// Updates the set.
        /// </summary>
        /// <param name="setId">The set identifier.</param>
        /// <param name="elementId">The element identifier.</param>
        /// <param name="dataElement">The data element.</param>
        /// <returns></returns>
        public async Task<JsonResult> UpdateElement(string setId, string elementId, DataElementType elementType, CreateUpdateElementOld dataElement)
        {
            using (var transaction = radElementDbContext.Database.BeginTransaction())
            {
                try
                {
                    if (IsValidSetId(setId) && IsValidElementId(elementId))
                    {
                        int id = Convert.ToInt32(setId.Remove(0, 4));
                        int elemId = Convert.ToInt32(elementId.Remove(0, 3));

                        if (dataElement == null)
                        {
                            return await Task.FromResult(new JsonResult("Element fields are invalid.", HttpStatusCode.BadRequest));
                        }

                        if (string.IsNullOrEmpty(dataElement.Label))
                        {
                            return await Task.FromResult(new JsonResult("'Label' field is missing in request", HttpStatusCode.BadRequest));
                        }

                        if (elementType == DataElementType.Choice || elementType == DataElementType.MultiChoice)
                        {
                            if (dataElement.Options == null)
                            {
                                return await Task.FromResult(new JsonResult("'Options' field is missing for Choice type elements in request", HttpStatusCode.BadRequest));
                            }
                        }

                        var elementSets = radElementDbContext.ElementSet.ToList();
                        var elementSet = elementSets.Find(x => x.Id == id);

                        if (elementSet != null)
                        {
                            var element = radElementDbContext.Element.ToList().Find(x => x.Id == elemId);
                            if (element != null)
                            {
                                var elementValues = radElementDbContext.ElementValue.ToList().Where(x => x.ElementId == element.Id);
                                if (elementValues != null && elementValues.Any())
                                {
                                    radElementDbContext.ElementValue.RemoveRange(elementValues);
                                }

                                element.Name = dataElement.Label;
                                element.ShortName = "";
                                element.Definition = dataElement.Definition ?? "";
                                element.MaxCardinality = 1;
                                element.MinCardinality = 1;
                                element.Source = "DSI TOUCH-AI";
                                element.Status = "Proposed";
                                element.StatusDate = DateTime.Now;
                                element.Editor = "";
                                element.Instructions = "";
                                element.Question = dataElement.Label ?? "";
                                element.References = "";
                                element.Synonyms = "";
                                element.VersionDate = DateTime.Now;
                                element.Version = "1";
                                element.ValueSize = 0;
                                element.Unit = "";

                                if (elementType == DataElementType.Integer)
                                {
                                    element.ValueType = "integer";
                                    element.ValueMin = dataElement.ValueMin;
                                    element.ValueMax = dataElement.ValueMax;
                                    element.StepValue = 1;
                                    element.Unit = dataElement.Unit ?? "";
                                }

                                if (elementType == DataElementType.Numeric)
                                {
                                    float? minValue = null;
                                    float? maxValue = null;

                                    if (dataElement.ValueMin.HasValue)
                                    {
                                        minValue = Convert.ToSingle(dataElement.ValueMin.Value);
                                    }

                                    if (dataElement.ValueMax.HasValue)
                                    {
                                        maxValue = Convert.ToSingle(dataElement.ValueMax.Value);
                                    }

                                    element.ValueType = "float";
                                    element.ValueMin = minValue;
                                    element.ValueMax = maxValue;
                                    element.StepValue = 0.1f;
                                    element.Unit = dataElement.Unit ?? "";
                                }

                                if (elementType == DataElementType.Choice)
                                {
                                    element.ValueType = "valueSet";
                                }

                                if (elementType == DataElementType.MultiChoice)
                                {
                                    element.ValueType = "valueSet";
                                    element.MaxCardinality = (uint)dataElement.Options.Count;
                                }

                                if (elementType == DataElementType.DateTime)
                                {
                                    element.ValueType = "date";
                                }

                                if (elementType == DataElementType.String)
                                {
                                    element.ValueType = "string";
                                }

                                if (elementType == DataElementType.MultiChoice || elementType == DataElementType.Choice)
                                {
                                    foreach (OptionOld option in dataElement.Options)
                                    {
                                        ElementValue elementvalue = new ElementValue()
                                        {
                                            Name = option.Label,
                                            Value = string.Empty,
                                            Definition = option.Label,
                                            ElementId = element.Id
                                        };

                                        radElementDbContext.ElementValue.Add(elementvalue);
                                    }
                                }

                                radElementDbContext.SaveChanges();
                                transaction.Commit();

                                return await Task.FromResult(new JsonResult(string.Format("Element with set id {0} and element id {1} is updated.", setId, elementId), HttpStatusCode.OK));
                            }
                        }
                    }

                    return await Task.FromResult(new JsonResult(string.Format("No such element with set id {0} and element id {1}.", setId, elementId), HttpStatusCode.NotFound));
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    logger.Error(ex, "Exception in method 'UpdateElement(int setId, int elementId, DataElementType elementType, CreateUpdateElement dataElement)'");
                    var exMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    return await Task.FromResult(new JsonResult(exMessage, HttpStatusCode.InternalServerError));
                }
            }
        }

        /// <summary>
        /// Updates the set.
        /// </summary>
        /// <param name="setId">The set identifier.</param>
        /// <param name="elementId">The element identifier.</param>
        /// <param name="dataElement">The data element.</param>
        /// <returns></returns>
        public async Task<JsonResult> UpdateElement(string setId, string elementId, UpdateElement dataElement)
        {
            using (var transaction = radElementDbContext.Database.BeginTransaction())
            {
                try
                {
                    if (IsValidSetId(setId) && IsValidElementId(elementId))
                    {
                        int setInternalId = Convert.ToInt32(setId.Remove(0, 4));
                        int elementInternalId = Convert.ToInt32(elementId.Remove(0, 3));

                        if (dataElement == null)
                        {
                            return await Task.FromResult(new JsonResult("Element fields are invalid.", HttpStatusCode.BadRequest));
                        }

                        if (dataElement.ValueType == DataElementType.Choice || dataElement.ValueType == DataElementType.MultiChoice)
                        {
                            if (dataElement.Options == null || dataElement.Options.Count == 0)
                            {
                                return await Task.FromResult(new JsonResult("'Options' field are missing for Choice type elements.", HttpStatusCode.BadRequest));
                            }
                        }

                        var elementSets = radElementDbContext.ElementSet.ToList();
                        var elementSet = elementSets.Find(x => x.Id == setInternalId);
                        var setElementRefs = radElementDbContext.ElementSetRef.ToList().Find(x => x.ElementId == elementInternalId && x.ElementSetId == setInternalId);

                        if (elementSet != null && setElementRefs != null)
                        {
                            var element = radElementDbContext.Element.ToList().Find(x => x.Id == elementInternalId);
                            if (element != null)
                            {
                                var elementValues = radElementDbContext.ElementValue.ToList().Where(x => x.ElementId == element.Id);
                                if (elementValues != null && elementValues.Any())
                                {
                                    radElementDbContext.ElementValue.RemoveRange(elementValues);
                                }

                                element.Name = dataElement.Name;
                                element.ShortName = dataElement.ShortName ?? string.Empty;
                                element.Definition = dataElement.Definition ?? string.Empty;
                                element.ValueType = GetElementValueType(dataElement.ValueType);
                                element.MinCardinality = 1;
                                element.MaxCardinality = (dataElement.ValueType == DataElementType.MultiChoice) ? (uint)dataElement.Options.Count : (uint)1;
                                element.Unit = dataElement.Unit ?? string.Empty;
                                element.Question = dataElement.Question ?? dataElement.Name;
                                element.Instructions = dataElement.Instructions ?? string.Empty;
                                element.References = dataElement.References ?? string.Empty;
                                element.Version = dataElement.Version ?? "";
                                element.VersionDate = DateTime.Now;
                                element.Synonyms = dataElement.Synonyms ?? string.Empty;
                                element.Source = dataElement.Source ?? string.Empty;
                                element.Status = "Proposed";
                                element.StatusDate = DateTime.Now;
                                element.Editor = dataElement.Editor ?? string.Empty;
                                element.Modality = dataElement.Modality != null && dataElement.Modality.Any() ? string.Join(",", dataElement.Modality) : null;
                                element.BiologicalSex = dataElement.BiologicalSex != null && dataElement.BiologicalSex.Any() ? string.Join(",", dataElement.BiologicalSex) : null;
                                element.AgeLowerBound = dataElement.AgeLowerBound;
                                element.AgeUpperBound = dataElement.AgeUpperBound;
                                element.ValueSize = 0;

                                if (dataElement.ValueType == DataElementType.Integer)
                                {
                                    element.ValueMin = dataElement.ValueMin;
                                    element.ValueMax = dataElement.ValueMax;
                                    element.StepValue = 1;
                                }

                                if (dataElement.ValueType == DataElementType.Numeric)
                                {
                                    float? minValue = null;
                                    float? maxValue = null;

                                    if (dataElement.ValueMin.HasValue)
                                    {
                                        minValue = Convert.ToSingle(dataElement.ValueMin.Value);
                                    }

                                    if (dataElement.ValueMax.HasValue)
                                    {
                                        maxValue = Convert.ToSingle(dataElement.ValueMax.Value);
                                    }

                                    element.ValueMin = minValue;
                                    element.ValueMax = maxValue;
                                    element.StepValue = 0.1f;
                                }

                                if (dataElement.ValueType == DataElementType.MultiChoice || dataElement.ValueType == DataElementType.Choice)
                                {
                                    AddElementValues(dataElement.Options, element.Id);
                                }

                                RemovePersonReferences((int)element.Id);
                                RemoveOrganizationReferences((int)element.Id);
                                AddPersonReferences((int)element.Id, dataElement.Persons);
                                AddOrganizationReferences((int)element.Id, dataElement.Organizations);

                                radElementDbContext.SaveChanges();
                                transaction.Commit();

                                return await Task.FromResult(new JsonResult(string.Format("Element with set id '{0}' and element id '{1}' is updated.", setId, elementId), HttpStatusCode.OK));
                            }
                        }
                    }

                    return await Task.FromResult(new JsonResult(string.Format("No such element with set id '{0}' and element id '{1}'.", setId, elementId), HttpStatusCode.NotFound));
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    logger.Error(ex, "Exception in method 'UpdateElement(string setId, string elementId, CreateUpdateElement dataElement)'");
                    var exMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    return await Task.FromResult(new JsonResult(exMessage, HttpStatusCode.InternalServerError));
                }
            }
        }

        /// <summary>
        /// Deletes the set.
        /// </summary>
        /// <param name="setId">The set identifier.</param>
        /// <param name="elementId">The element identifier.</param>
        /// <returns></returns>
        public async Task<JsonResult> DeleteElement(string setId, string elementId)
        {
            using (var transaction = radElementDbContext.Database.BeginTransaction())
            {
                try
                {
                    if (IsValidSetId(setId) && IsValidElementId(elementId))
                    {
                        int setInternalId = Convert.ToInt32(setId.Remove(0, 4));
                        int elementInternalId = Convert.ToInt32(elementId.Remove(0, 3));
                        var elementSetRefs = radElementDbContext.ElementSetRef.ToList();
                        var elementSetRef = elementSetRefs.Find(x => x.ElementSetId == setInternalId && x.ElementId == elementInternalId);

                        if (elementSetRef != null)
                        {
                            var elementValues = radElementDbContext.ElementValue.ToList().Where(x => x.ElementId == elementSetRef.ElementId);
                            var element = radElementDbContext.Element.ToList().Find(x => x.Id == elementSetRef.ElementId);
                            
                            if (element != null)
                            {
                                radElementDbContext.Element.Remove(element);
                            }

                            RemoveElementValues(elementValues);
                            RemoveElementSetReferences(elementSetRef);
                            RemovePersonReferences(elementInternalId);
                            RemoveOrganizationReferences(elementInternalId);

                            radElementDbContext.SaveChanges();
                            transaction.Commit();

                            return await Task.FromResult(new JsonResult(string.Format("Element with set id '{0}' and element id '{1}' is deleted.", setId, elementId), HttpStatusCode.OK));
                        }
                    }
                    return await Task.FromResult(new JsonResult(string.Format("No such element with set id '{0}' and element id '{1}'.", setId, elementId), HttpStatusCode.NotFound));
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    logger.Error(ex, "Exception in method 'DeleteElement(string setId, string elementId)'");
                    var exMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    return await Task.FromResult(new JsonResult(exMessage, HttpStatusCode.InternalServerError));
                }
            }
        }

        /// <summary>
        /// Adds the element values.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="elementId">The element identifier.</param>
        private void AddElementValues(List<Option> options, uint elementId)
        {
            foreach (Option option in options)
            {
                ElementValue elementvalue = new ElementValue()
                {
                    ElementId = elementId,
                    Value = option.Value ?? "",
                    Name = option.Name ?? "",
                    Definition = option.Definition ?? "",
                    Images = option.Images ?? ""
                };

                radElementDbContext.ElementValue.Add(elementvalue);
            }
        }

        /// <summary>
        /// Removes the element values.
        /// </summary>
        /// <param name="elementValues">The element values.</param>
        private void RemoveElementValues(IEnumerable<ElementValue> elementValues)
        {
            if (elementValues != null && elementValues.Any())
            {
                radElementDbContext.ElementValue.RemoveRange(elementValues);
            };
        }

        /// <summary>
        /// Adds the element set references.
        /// </summary>
        /// <param name="setId">The set identifier.</param>
        /// <param name="elementId">The element identifier.</param>
        private void AddElementSetReferences(int setId, int elementId)
        {
            ElementSetRef setRef = new ElementSetRef()
            {
                ElementSetId = setId,
                ElementId = elementId
            };

            radElementDbContext.ElementSetRef.Add(setRef);
        }

        /// <summary>
        /// Removes the element set references.
        /// </summary>
        /// <param name="setRef">The set reference.</param>
        private void RemoveElementSetReferences(ElementSetRef setRef)
        {
            radElementDbContext.ElementSetRef.Remove(setRef);
        }

        /// <summary>
        /// Adds the person references.
        /// </summary>
        /// <param name="elementId">The element identifier.</param>
        /// <param name="personRefs">The person refs.</param>
        private void AddPersonReferences(int elementId, List<PersonDetails> personRefs)
        {
            if (personRefs != null && personRefs.Any())
            {
                var persons = radElementDbContext.Person.ToList();

                foreach (var personRef in personRefs)
                {
                    var person = persons.Find(x => x.Id == personRef.PersonId);
                    if (person != null)
                    {
                        if (personRef.Roles != null && personRef.Roles.Any())
                        {
                            foreach (var role in personRef.Roles.Distinct())
                            {
                                var setRef = new PersonRoleElementRef()
                                {
                                    ElementID = elementId,
                                    PersonID = personRef.PersonId,
                                    Role = role.ToString()
                                };

                                radElementDbContext.PersonRoleElementRef.Add(setRef);
                            }
                        }
                        else
                        {
                            var setRef = new PersonRoleElementRef()
                            {
                                ElementID = elementId,
                                PersonID = personRef.PersonId
                            };

                            radElementDbContext.PersonRoleElementRef.Add(setRef);
                        }
                    }
                }
                radElementDbContext.SaveChanges();
            }
        }

        /// <summary>
        /// Adds the organization references.
        /// </summary>
        /// <param name="elementId">The set identifier.</param>
        /// <param name="orgRefs">The org refs.</param>
        private void AddOrganizationReferences(int elementId, List<OrganizationDetails> orgRefs)
        {
            if (orgRefs != null && orgRefs.Any())
            {
                var organizations = radElementDbContext.Organization.ToList();

                foreach (var orgRef in orgRefs)
                {
                    var organization = organizations.Find(x => x.Id == orgRef.OrganizationId);
                    if (organization != null)
                    {
                        if (orgRef.Roles != null && orgRef.Roles.Any())
                        {
                            foreach (var role in orgRef.Roles.Distinct())
                            {
                                var setRef = new OrganizationRoleElementRef()
                                {
                                    ElementID = elementId,
                                    OrganizationID = orgRef.OrganizationId,
                                    Role = role.ToString()
                                };

                                radElementDbContext.OrganizationRoleElementRef.Add(setRef);
                            }
                        }
                        else
                        {
                            var setRef = new OrganizationRoleElementRef()
                            {
                                ElementID = elementId,
                                OrganizationID = orgRef.OrganizationId
                            };

                            radElementDbContext.OrganizationRoleElementRef.Add(setRef);
                        }
                    }
                }
                radElementDbContext.SaveChanges();
            }
        }

        /// <summary>
        /// Removes the person elements references.
        /// </summary>
        /// <param name="elementId">The element identifier.</param>
        private void RemovePersonReferences(int elementId)
        {
            var personElementsRefs = radElementDbContext.PersonRoleElementRef.ToList().Where(x => x.ElementID == elementId);
            if (personElementsRefs != null && personElementsRefs.Any())
            {
                radElementDbContext.PersonRoleElementRef.RemoveRange(personElementsRefs);
                radElementDbContext.SaveChanges();
            }
        }

        /// <summary>
        /// Removes the organization element references.
        /// </summary>
        /// <param name="elementId">The element identifier.</param>
        private void RemoveOrganizationReferences(int elementId)
        {
            var organizationElementsRefs = radElementDbContext.OrganizationRoleElementRef.ToList().Where(x => x.ElementID == elementId);
            if (organizationElementsRefs != null && organizationElementsRefs.Any())
            {
                radElementDbContext.OrganizationRoleElementRef.RemoveRange(organizationElementsRefs);
                radElementDbContext.SaveChanges();
            }
        }

        /// <summary>
        /// Determines whether [is valid element identifier] [the specified element identifier].
        /// </summary>
        /// <param name="elementId">The element identifier.</param>
        /// <returns>
        ///   <c>true</c> if [is valid element identifier] [the specified element identifier]; otherwise, <c>false</c>.
        /// </returns>
        private bool IsValidElementId(string elementId)
        {
            if (elementId.Length > 3 && string.Equals(elementId.Substring(0, 3), "RDE", StringComparison.OrdinalIgnoreCase))
            {
                bool result = int.TryParse(elementId.Remove(0, 3), out _);
                return result;
            }

            return false;
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
            if (setId.Length > 4 && string.Equals(setId.Substring(0, 4), "RDES", StringComparison.OrdinalIgnoreCase))
            {
                bool result = int.TryParse(setId.Remove(0, 4), out _);
                return result;
            }

            return false;
        }

        /// <summary>
        /// Gets the element details dto.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns></returns>
        private object GetElementDetailsDto(object element)
        {
            if (element.GetType() == typeof(List<Element>))
            {
                var elements = mapper.Map<List<Element>, List<ElementDetails>>(element as List<Element>);
                elements.ForEach(_element =>
                {
                    _element.SetInformation = GetSetDetails((_element as Element).Id);
                    _element.OrganizationInformation = GetOrganizationDetails((int)(_element as Element).Id);
                    _element.PersonInformation = GetPersonDetails((int)(_element as Element).Id);

                    if (_element.ValueType == "valueSet")
                    {
                        _element.ElementValues = GetElementValues((_element as Element).Id);
                    }
                });

                return elements;
            }
            else if (element.GetType() == typeof(Element))
            {
                var elementDetails = mapper.Map<ElementDetails>(element as Element);
                elementDetails.SetInformation = GetSetDetails((element as Element).Id);
                elementDetails.OrganizationInformation = GetOrganizationDetails((int)(element as Element).Id);
                elementDetails.PersonInformation = GetPersonDetails((int)(element as Element).Id);

                if (elementDetails.ValueType == "valueSet")
                {
                    elementDetails.ElementValues = GetElementValues((element as Element).Id);
                }
                return elementDetails;
            }

            return null;
        }

        /// <summary>
        /// Gets the element values.
        /// </summary>
        /// <param name="elementId">The element identifier.</param>
        /// <returns></returns>
        private List<ElementValue> GetElementValues(uint elementId)
        {
            return radElementDbContext.ElementValue.ToList().Where(x => x.ElementId == elementId).ToList();
        }

        /// <summary>
        /// Gets the set details.
        /// </summary>
        /// <param name="elementId">The element identifier.</param>
        /// <returns></returns>
        private List<SetBasicAttributes> GetSetDetails(uint elementId)
        {
            List<SetBasicAttributes> setInfo = new List<SetBasicAttributes>();
            var elementSetRefs = radElementDbContext.ElementSetRef.ToList().Where(x => x.ElementId == elementId);
            if (elementSetRefs != null && elementSetRefs.Any())
            {
                foreach (var setRef in elementSetRefs)
                {
                    var set = radElementDbContext.ElementSet.ToList().Where(x => x.Id == setRef.ElementSetId).FirstOrDefault();
                    if (set != null)
                    {
                        setInfo.Add(new SetBasicAttributes
                        {
                            SetId = string.Concat("RDES", set.Id),
                            SetName = set.Name
                        }); ;
                    }
                }
            }

            return setInfo;
        }

        /// <summary>
        /// Gets the organization details.
        /// </summary>
        /// <param name="setId">The set identifier.</param>
        /// <returns></returns>
        private List<OrganizationAttributes> GetOrganizationDetails(int setId)
        {
            List<OrganizationAttributes> organizationInfo = new List<OrganizationAttributes>();
            var organizationElementSetRefs = radElementDbContext.OrganizationRoleElementRef.ToList().Where(x => x.ElementID == setId);
            if (organizationElementSetRefs != null && organizationElementSetRefs.Any())
            {
                foreach (var organizationElementSetRef in organizationElementSetRefs)
                {
                    var organization = radElementDbContext.Organization.ToList().Where(x => x.Id == organizationElementSetRef.OrganizationID).FirstOrDefault();
                    if (organization != null)
                    {
                        if (!organizationInfo.Exists(x => x.Id == organization.Id))
                        {
                            var organizationDetails = mapper.Map<OrganizationAttributes>(organization);
                            organizationDetails.Roles.Add(organizationElementSetRef.Role);
                            organizationInfo.Add(organizationDetails);
                        }
                        else
                        {
                            var existingOrganization = organizationInfo.Find(x => x.Id == organization.Id);
                            existingOrganization.Roles.Add(organizationElementSetRef.Role);
                        }
                    }
                }
            }

            return organizationInfo;
        }

        /// <summary>
        /// Gets the person details.
        /// </summary>
        /// <param name="setId">The set identifier.</param>
        /// <returns></returns>
        private List<PersonAttributes> GetPersonDetails(int setId)
        {
            List<PersonAttributes> personInfo = new List<PersonAttributes>();
            var personElementSetRefs = radElementDbContext.PersonRoleElementRef.ToList().Where(x => x.ElementID == setId);
            if (personElementSetRefs != null && personElementSetRefs.Any())
            {
                foreach (var personElementSetRef in personElementSetRefs)
                {
                    var person = radElementDbContext.Person.ToList().Where(x => x.Id == personElementSetRef.PersonID).FirstOrDefault();
                    if (person != null)
                    {
                        if (!personInfo.Exists(x => x.Id == person.Id))
                        {
                            var personDetails = mapper.Map<PersonAttributes>(person);
                            personDetails.Roles.Add(personElementSetRef.Role);
                            personInfo.Add(personDetails);
                        }
                        else
                        {
                            var existingPerson = personInfo.Find(x => x.Id == person.Id);
                            existingPerson.Roles.Add(personElementSetRef.Role);
                        }
                    }
                }
            }

            return personInfo;
        }

        /// <summary>
        /// Gets the type of the element value.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        private string GetElementValueType(DataElementType? type)
        {
            var valueType = string.Empty;

            switch (type)
            {
                case DataElementType.Choice:
                case DataElementType.MultiChoice:
                    valueType = "valueSet";
                    break;

                case DataElementType.Integer:
                    valueType = "integer";
                    break;

                case DataElementType.Numeric:
                    valueType = "float";
                    break;

                case DataElementType.DateTime:
                    valueType = "date";
                    break;

                case DataElementType.String:
                    valueType = "string";
                    break;
            }

            return valueType;
        }
    }
}
