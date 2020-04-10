﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RadElement.Core.DTO
{
    public class CreateUpdateSet
    {
        /// <summary>
        /// Gets or sets the module name.
        /// </summary>
        [Required]
        public string ModuleName { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        [Required]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the name of the contact.
        /// </summary>
        [Required]
        public string ContactName { get; set; }

        /// <summary>
        /// Gets or sets the parent identifier.
        /// </summary>
        public int? ParentId { get; set; }

        /// <summary>
        /// Gets or sets the modality.
        /// </summary>
        public List<ModalityType> Modality { get; set; }

        /// <summary>
        /// Gets or sets the Biological sex.
        /// </summary>
        public List<BiologicalSex> BiologicalSex { get; set; }

        /// <summary>
        /// Gets or sets the age upper bound.
        /// </summary>
        public float? AgeUpperBound { get; set; }

        /// <summary>
        /// Gets or sets the age lower bound.
        /// </summary>
        public float? AgeLowerBound { get; set; }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        [MaxLength(8)]
        public string Version { get; set; }
    }
}
