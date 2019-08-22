using System;
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
    public class PersistedGrantStore : IPersistedGrantStore
    {
        private readonly IPersistedGrantDbContext _context;
        private readonly ILogger _logger;

        public PersistedGrantStore(IPersistedGrantDbContext context, ILogger<PersistedGrantStore> logger)
        {
            Guard.ForNull(context, nameof(context));
            Guard.ForNull(logger, nameof(logger));

            _context = context;
            _logger = logger;
        }

        public async Task StoreAsync(PersistedGrant token)
        {
            try
            {
                var existing = (await _context.PersistedGrants(x => x.Key == token.Key)).ToList().SingleOrDefault();

                if (existing == null)
                {
                    _logger.LogDebug("{persistedGrantKey} not found in database", token.Key);

                    var persistedGrant = token.ToEntity();
                    await _context.Add(persistedGrant);
                }
                else
                {
                    _logger.LogDebug("{persistedGrantKey} found in database", token.Key);

                    token.UpdateEntity(existing);
                    await _context.Update(x => x.Key == token.Key, existing);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(0, ex, "Exception storing persisted grant");
            }
        }

        public async Task<PersistedGrant> GetAsync(string key)
        {
            var persistedGrant = (await _context.PersistedGrants(x => x.Key == key)).ToList();

            PersistedGrant model = persistedGrant.FirstOrDefault()?.ToModel();

            _logger.LogDebug($"{key} found in database: {model != null}");

            return model;
        }

        public async Task<IEnumerable<PersistedGrant>> GetAllAsync(string subjectId)
        {
            var persistedGrants = (await _context.PersistedGrants(x => x.SubjectId == subjectId)).ToList();

            var model = persistedGrants.Select(x => x.ToModel());

            _logger.LogDebug($"{persistedGrants.Count} persisted grants found for {subjectId}");

            return model;
        }

        public Task RemoveAsync(string key)
        {
            _logger.LogDebug($"removing {key} persisted grant from database");

            _context.Remove(x => x.Key == key);

            return Task.FromResult(0);
        }

        public Task RemoveAllAsync(string subjectId, string clientId)
        {
            _logger.LogDebug($"removing persisted grants from database for subject {subjectId}, clientId {clientId}");

            _context.Remove(x => x.SubjectId == subjectId && x.ClientId == clientId);

            return Task.FromResult(0);
        }

        public Task RemoveAllAsync(string subjectId, string clientId, string type)
        {
            _logger.LogDebug(
                $"removing persisted grants from database for subject {subjectId}, clientId {clientId}, grantType {type}");

            _context.Remove(
                x =>
                    x.SubjectId == subjectId &&
                    x.ClientId == clientId &&
                    x.Type == type);

            return Task.FromResult(0);
        }
    }
}