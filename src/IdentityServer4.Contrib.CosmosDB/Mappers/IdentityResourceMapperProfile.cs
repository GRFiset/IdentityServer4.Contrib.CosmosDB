﻿using System.Linq;
using AutoMapper;
using IdentityServer4.Contrib.CosmosDB.Entities;

namespace IdentityServer4.Contrib.CosmosDB.Mappers
{
    /// <inheritdoc />
    /// <summary>
    ///     AutoMapper configuration for identity resource
    ///     Between model and entity
    /// </summary>
    public class IdentityResourceMapperProfile : Profile
    {
        /// <summary>
        ///     <see cref="IdentityResourceMapperProfile" />
        /// </summary>
        public IdentityResourceMapperProfile()
        {
            // entity to model
            CreateMap<IdentityResource, Models.IdentityResource>(MemberList.Destination)
                .ForMember(x => x.UserClaims, opt => opt.MapFrom(src => src.UserClaims.Select(x => x.Type)));

            // model to entity
            CreateMap<Models.IdentityResource, IdentityResource>(MemberList.Source)
                .ForMember(x => x.Kind, opt => opt.MapFrom(o => typeof(IdentityResource).Name))
                .ForMember(x => x.UserClaims,
                    opts => opts.MapFrom(src => src.UserClaims.Select(x => new IdentityClaim {Type = x})));
        }
    }
}