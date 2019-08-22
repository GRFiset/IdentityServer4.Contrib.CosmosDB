using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using IdentityServer4.Contrib.CosmosDB.Entities;

namespace IdentityServer4.Contrib.CosmosDB.Interfaces
{
    public interface IPersistedGrantDbContext : IDisposable
    {
        Task<IEnumerable<PersistedGrant>> PersistedGrants(Expression<Func<PersistedGrant, bool>> predicate = null, string partitionKey = "");

        Task Add(PersistedGrant entity);

        Task Update(PersistedGrant entity);
        Task Update(Expression<Func<PersistedGrant, bool>> filter, PersistedGrant entity);

        Task Remove(PersistedGrant entity);
        Task Remove(Expression<Func<PersistedGrant, bool>> filter);

        Task RemoveExpired();

        Task SetupPersistedGrants();
    }
}