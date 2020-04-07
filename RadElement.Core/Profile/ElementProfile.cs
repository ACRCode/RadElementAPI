﻿using RadElement.Core.Domain;
using RadElement.Core.DTO;

namespace RadElement.Core.Profile
{
    public class ElementProfile : AutoMapper.Profile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ElementSetProfile" /> class.
        /// </summary>
        public ElementProfile()
        {
            CreateMap<Element, ElementDetails>();
        }
    }
}
