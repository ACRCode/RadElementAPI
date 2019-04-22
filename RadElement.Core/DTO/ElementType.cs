﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RadElement.Core.DTO
{
    /// <summary>
    /// Gets or sets the Data element type
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ElementType
    {
        /// <summary>
        /// Data Element Type is Integer
        /// </summary>
        Integer,

        /// <summary>
        /// Data Element Type is Numeric
        /// </summary>
        Numeric,

        /// <summary>
        /// Data Element Type is Choice
        /// </summary>
        Choice,

        /// <summary>
        /// Data Element Type is MultiChoice
        /// </summary>
        MultiChoice,

        /// <summary>
        /// Data Element Type is DateTime
        /// </summary>
        DateTime,

        ///// <summary>
        ///// Data Element Type is Duration
        ///// </summary>
        //Duration,

        /// <summary>
        /// Data Element Type is String
        /// </summary>
        String,
    }
}
