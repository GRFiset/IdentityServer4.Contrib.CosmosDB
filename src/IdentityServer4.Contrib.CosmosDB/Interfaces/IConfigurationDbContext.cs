using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace IdentityServer4.Contrib.CosmosDB.Interfaces
{
    public interface IConfigurationDbContext : IDisposable
    {
        Task<IEnumerable<T>> GetDocument<T>(Expression<Func<T, bool>> predicate = null);

        Task AddDocument<T>(T entity);

        Task EnsureConfigurationsCollectionCreated();
    }
}