﻿using Acr.Assist.RadElement.Core.Domain;

namespace Acr.Assist.RadElement.Core.DTO
{
    /// <summary>
    /// Represents a Integer elemeny
    /// </summary>
    public class IntegerElement : DataElement
    {    
        public IntegerElement()
        {
            DataElementType = DataElementType.Integer;
        }

        /// <summary>
        /// Represents the minimum value
        /// </summary>
        public int? MinimumValue { get; set; }

        /// <summary>
        /// Represents the maximum value
        /// </summary>
        public int? MaximumValue { get; set; }
    }
}