﻿using System;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Contrib.CosmosDB.Entities;
using IdentityServer4.Contrib.CosmosDB.Interfaces;
using IdentityServer4.Services;
using Microsoft.Extensions.Logging;

namespace IdentityServer4.Contrib.CosmosDB.Services
{
    public class CorsPolicyService : ICorsPolicyService
    {
        private readonly IConfigurationDbContext _context;
        private readonly ILogger _logger;

        public CorsPolicyService(IConfigurationDbContext context, ILogger<CorsPolicyService> logger)
        {
            Guard.ForNull(context, nameof(context));
            Guard.ForNull(logger, nameof(logger));

            _context = context;
            _logger = logger;
        }

        public async Task<bool> IsOriginAllowedAsync(string origin)
        {
            // If we use SelectMany directly, we got a Unsupported Exception inside CosmosDb.
            var clients = await _context.GetDocument<Client>();
            var distinctOrigins = clients.Where(x => x != null)
                .SelectMany(x => x.AllowedCorsOrigins.Select(y => y.Origin))
                .Distinct()
                .ToList();

            var isAllowed = distinctOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);

            _logger.LogDebug("Origin {origin} is allowed: {originAllowed}", origin, isAllowed);

            return isAllowed;
        }
    }
}