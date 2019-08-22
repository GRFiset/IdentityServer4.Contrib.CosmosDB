using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using IdentityServer4.Contrib.CosmosDB.Extensions;
using IdentityServer4.Contrib.CosmosDB.Interfaces;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Logging;

namespace IdentityServer4.Contrib.CosmosDB.Stores
{
    public class ClientStore : IClientStore
    {
        private readonly IConfigurationDbContext _context;
        private readonly ILogger _logger;

        public ClientStore(IConfigurationDbContext context, ILogger<ClientStore> logger)
        {
            Guard.ForNull(context, nameof(context));
            Guard.ForNull(logger, nameof(logger));

            _context = context;
            _logger = logger;
        }

        public async Task<Client> FindClientByIdAsync(string clientId)
        {
            IEnumerable<Entities.Client> clients = await _context.GetDocument<Entities.Client>(c => c.ClientId == clientId);

            Client model = clients?.ToList().FirstOrDefault().ToModel();

            _logger.LogDebug($"{clientId} found in database: {model != null}");

            return model;
        }
    }
}