using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Contrib.CosmosDB.Extensions;
using IdentityServer4.Contrib.CosmosDB.Interfaces;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Logging;

namespace IdentityServer4.Contrib.CosmosDB.Stores
{
    public class ResourceStore : IResourceStore
    {
        private readonly IConfigurationDbContext _context;
        private readonly ILogger _logger;

        public ResourceStore(IConfigurationDbContext context, ILogger<ResourceStore> logger)
        {
            Guard.ForNull(context, nameof(context));
            Guard.ForNull(logger, nameof(logger));

            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeAsync(IEnumerable<string> scopeNames)
        {
            var scopes = scopeNames.ToArray();

            var resources = await _context.GetDocument<Entities.IdentityResource>(x => scopes.Contains(x.Name));

            _logger.LogDebug("Found {scopes} identity scopes in database", resources.Select(x => x.Name));

            return resources.Select(x => x.ToModel()).ToArray().AsEnumerable();
        }

        public async Task<IEnumerable<ApiResource>> FindApiResourcesByScopeAsync(IEnumerable<string> scopeNames)
        {
            var names = scopeNames.ToArray();

            var apis = await _context.GetDocument<Entities.ApiResource>(x => names.Contains(x.Name));

            var models = apis.Select(x => x.ToModel()).ToArray();

            _logger.LogDebug("Found {scopes} API scopes in database",
                models.SelectMany(x => x.Scopes).Select(x => x.Name));

            return models.AsEnumerable();
        }

        public async Task<ApiResource> FindApiResourceAsync(string name)
        {
            IEnumerable<Entities.ApiResource> apis = await _context.GetDocument<Entities.ApiResource>(a => a.Name == name);

            var api = apis.FirstOrDefault();

            if (api != null)
                _logger.LogDebug($"Found {name} API resource in database");
            else
                _logger.LogDebug($"Did not find {name} API resource in database");

            return api.ToModel();
        }

        public async Task<Resources> GetAllResourcesAsync()
        {
            IEnumerable<Entities.IdentityResource> identity = await _context.GetDocument<Entities.IdentityResource>();

            IEnumerable<Entities.ApiResource> apis = await _context.GetDocument<Entities.ApiResource>();

            var result = new Resources(
                identity.ToArray().Select(x => x.ToModel()).AsEnumerable(),
                apis.ToArray().Select(x => x.ToModel()).AsEnumerable());

            _logger.LogDebug("Found {scopes} as all scopes in database",
                result.IdentityResources.Select(x => x.Name)
                    .Union(result.ApiResources.SelectMany(x => x.Scopes).Select(x => x.Name)));

            return result;
        }
    }
}