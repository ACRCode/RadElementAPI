﻿using Newtonsoft.Json;
using System;

namespace RadElement.Core.Domain
{
    public class SpecialtyElementSetRef
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the element set identifier.
        /// </summary>
        public int ElementSetId { get; set; }

        /// <summary>
        /// Gets or sets the specialty identifier.
        /// </summary>
        public int SpecialtyId { get; set; }

        /// <summary>
        /// Gets or sets the deleted at.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? Deleted_At { get; set; }
    }
}
