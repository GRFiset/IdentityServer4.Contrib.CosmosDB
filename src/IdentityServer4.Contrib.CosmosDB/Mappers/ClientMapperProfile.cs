﻿using System.Linq;
using System.Security.Claims;
using AutoMapper;
using IdentityServer4.Contrib.CosmosDB.Entities;
using Secret = IdentityServer4.Models.Secret;

namespace IdentityServer4.Contrib.CosmosDB.Mappers
{
    /// <inheritdoc />
    /// <summary>
    ///     AutoMapper configuration for Client
    ///     Between model and entity
    /// </summary>
    public class ClientMapperProfile : Profile
    {
        /// <summary>
        ///     <see>
        ///         <cref>{ClientMapperProfile}</cref>
        ///     </see>
        /// </summary>
        public ClientMapperProfile()
        {
            // entity to model
            CreateMap<Client, Models.Client>(MemberList.Destination)
                .ForMember(x => x.Properties,
                    opt => opt.MapFrom(src => src.Properties.ToDictionary(item => item.Key, item => item.Value)))
                .ForMember(x => x.AllowedGrantTypes,
                    opt => opt.MapFrom(src => src.AllowedGrantTypes.Select(x => x.GrantType)))
                .ForMember(x => x.RedirectUris, opt => opt.MapFrom(src => src.RedirectUris.Select(x => x.RedirectUri)))
                .ForMember(x => x.PostLogoutRedirectUris,
                    opt => opt.MapFrom(src => src.PostLogoutRedirectUris.Select(x => x.PostLogoutRedirectUri)))
                .ForMember(x => x.AllowedScopes, opt => opt.MapFrom(src => src.AllowedScopes.Select(x => x.Scope)))
                .ForMember(x => x.ClientSecrets, opt => opt.MapFrom(src => src.ClientSecrets.Select(x => x)))
                .ForMember(x => x.Claims, opt => opt.MapFrom(src => src.Claims.Select(x => new Claim(x.Type, x.Value))))
                .ForMember(x => x.IdentityProviderRestrictions,
                    opt => opt.MapFrom(src => src.IdentityProviderRestrictions.Select(x => x.Provider)))
                .ForMember(x => x.AllowedCorsOrigins,
                    opt => opt.MapFrom(src => src.AllowedCorsOrigins.Select(x => x.Origin)));

            CreateMap<ClientSecret, Secret>(MemberList.Destination)
                .ForMember(dest => dest.Type, opt => opt.Condition(srs => srs != null));

            // model to entity
            CreateMap<Models.Client, Client>(MemberList.Source)
                .ForMember(x=> x.Kind, opt => opt.MapFrom(o => typeof(Client).Name))
                .ForMember(x => x.Properties,
                    opt => opt.MapFrom(src =>
                        src.Properties.ToList().Select(x => new ClientProperty {Key = x.Key, Value = x.Value})))
                .ForMember(x => x.AllowedGrantTypes,
                    opt => opt.MapFrom(src => src.AllowedGrantTypes.Select(x => new ClientGrantType {GrantType = x})))
                .ForMember(x => x.RedirectUris,
                    opt => opt.MapFrom(src => src.RedirectUris.Select(x => new ClientRedirectUri {RedirectUri = x})))
                .ForMember(x => x.PostLogoutRedirectUris,
                    opt =>
                        opt.MapFrom(
                            src =>
                                src.PostLogoutRedirectUris.Select(
                                    x => new ClientPostLogoutRedirectUri {PostLogoutRedirectUri = x})))
                .ForMember(x => x.AllowedScopes,
                    opt => opt.MapFrom(src => src.AllowedScopes.Select(x => new ClientScope {Scope = x})))
                .ForMember(x => x.Claims,
                    opt => opt.MapFrom(src => src.Claims.Select(x => new ClientClaim {Type = x.Type, Value = x.Value})))
                .ForMember(x => x.IdentityProviderRestrictions,
                    opt =>
                        opt.MapFrom(
                            src => src.IdentityProviderRestrictions.Select(x => new ClientIdPRestriction
                                {Provider = x})))
                .ForMember(x => x.AllowedCorsOrigins,
                    opt => opt.MapFrom(src => src.AllowedCorsOrigins.Select(x => new ClientCorsOrigin {Origin = x})));
            CreateMap<Secret, ClientSecret>(MemberList.Source);
        }
    }
}