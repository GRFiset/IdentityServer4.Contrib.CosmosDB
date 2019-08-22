using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;
using IdentityServer4.Contrib.CosmosDB.Abstracts;
using IdentityServer4.Contrib.CosmosDB.Configuration;
using IdentityServer4.Contrib.CosmosDB.Entities;
using IdentityServer4.Contrib.CosmosDB.Extensions;
using IdentityServer4.Contrib.CosmosDB.Interfaces;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IdentityServer4.Contrib.CosmosDB.DbContext
{
    /// <inheritdoc cref="CosmosDbContextBase" />
    /// <summary>
    ///     Persisted Grant DbContext Class.
    /// </summary>
    public class PersistedGrantDbContext : CosmosDbContextBase, IPersistedGrantDbContext
    {
        private DocumentCollection _persistedGrants;
        private Uri _persistedGrantsUri;
        private string databaseId;
        /// <summary>
        ///     Create an instance of the PersistedGrantDbContext Class.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="databaseName"></param>
        /// <param name="connectionPolicy"></param>
        /// <param name="logger"></param>
        public PersistedGrantDbContext(IOptions<CosmosDbConfiguration> settings,
                                       ICosmosClient cosmosClient,
            string databaseName = Constants.DatabaseName,
            ConnectionPolicy connectionPolicy = null,
            ILogger<PersistedGrantDbContext> logger = null)
            : base(settings, cosmosClient, logger)
        {
            Guard.ForNullOrDefault(settings.Value, nameof(settings));

            databaseId = Configuration.DatabaseName.GetValueOrDefault(databaseName);
            _persistedGrantsUri =
                UriFactory.CreateDocumentCollectionUri(databaseId, Constants.CollectionNames.PersistedGrant);
            Logger?.LogDebug($"Persisted Grants URI: {_persistedGrantsUri}");

            var partitionKeyDefinition = new PartitionKeyDefinition
                { Paths = { Constants.CollectionPartitionKeys.PersistedGrant } };
            Logger?.LogDebug($"Persisted Grants Partition Key: {partitionKeyDefinition}");

            var indexingPolicy = new IndexingPolicy
            {
                Automatic = true,
                IndexingMode = IndexingMode.Consistent,
                IncludedPaths =
                {
                    new IncludedPath
                    {
                        Path = "/expiration/?",
                        Indexes =
                        {
                            Index.Range(DataType.String)
                        }
                    },
                    new IncludedPath
                    {
                        Path = "/",
                        Indexes = {Index.Range(DataType.String)}
                    }
                }
            };
            Logger?.LogDebug($"Persisted Grants Indexing Policy: {indexingPolicy}");

            _persistedGrants = new DocumentCollection
            {
                Id = Constants.CollectionNames.PersistedGrant,
                PartitionKey = partitionKeyDefinition,
                IndexingPolicy = indexingPolicy
            };
        }


        /// <summary>
        ///     Add new Persisted Grant.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public async Task Add(PersistedGrant entity)
        {
            await DocumentClient.CreateDocumentAsync(_persistedGrantsUri, entity);
        }

        /// <summary>
        ///     Remove multiple Persisted Grants.
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public async Task Remove(Expression<Func<PersistedGrant, bool>> filter)
        {
            IEnumerable<PersistedGrant> persistedGrants = await PersistedGrants(filter);
            foreach (var persistedGrant in persistedGrants)
            {
                await Remove(persistedGrant);
            }
        }

        /// <summary>
        ///     Removed expired Persisted Grants.
        /// </summary>
        /// <returns></returns>
        public async Task RemoveExpired()
        {
            await Remove(x => x.Expiration < DateTime.UtcNow);
        }

        /// <summary>
        ///     Updated a Persisted Grant.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public async Task Update(PersistedGrant entity)
        {
            var documentUrl = UriFactory.CreateDocumentUri(databaseId, _persistedGrants.Id, entity.Id);
            await DocumentClient.ReplaceDocumentAsync(documentUrl, entity);
        }

        /// <summary>
        ///     Update multiple Persisted Grants.
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public async Task Update(Expression<Func<PersistedGrant, bool>> filter, PersistedGrant entity)
        {
            // TODO : This looks like its a MongoDb specific thing.  This is an attempt to match it.
            // await _persistedGrants.ReplaceOneAsync(filter, entity);
            await DocumentClient.UpsertDocumentAsync(_persistedGrantsUri, entity);
        }

        /// <summary>
        ///     Remove a Persisted Grant.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public async Task Remove(PersistedGrant entity)
        {
            Uri documentUrl = UriFactory.CreateDocumentUri(databaseId, _persistedGrants.Id, entity.Id);
            await DocumentClient.DeleteDocumentAsync(documentUrl, new RequestOptions { PartitionKey = new PartitionKey(entity.ClientId) });
        }

        /// <summary>
        ///     Queryable Persisted Grants.
        /// </summary>
        public async Task<IEnumerable<PersistedGrant>> PersistedGrants(Expression<Func<PersistedGrant, bool>> predicate = null, string partitionKey = "")
        {
            FeedOptions feedOptions;
            if (string.IsNullOrWhiteSpace(partitionKey))
            {
                feedOptions = new FeedOptions
                {
                    EnableCrossPartitionQuery = true
                };
            }
            else
            {
                feedOptions = new FeedOptions
                {
                    PartitionKey = new PartitionKey(partitionKey)
                };
            }

            IDocumentQuery<PersistedGrant> query;
            if (predicate == null)
            {
                query = DocumentClient.CreateDocumentQuery<PersistedGrant>(_persistedGrantsUri, feedOptions)
                    .AsDocumentQuery();
            }
            else
            {
                query = DocumentClient.CreateDocumentQuery<PersistedGrant>(_persistedGrantsUri, feedOptions)
                    .Where(predicate)
                    .AsDocumentQuery();
            }

            var results = new List<PersistedGrant>();
            while (query.HasMoreResults)
            {
                FeedResponse<PersistedGrant> res = await query.ExecuteNextAsync<PersistedGrant>();
                results.AddRange(res);
            }

            return results;
        }

        public async Task SetupPersistedGrants()
        {
            EnsureDatabaseCreated(Constants.DatabaseName);

            _persistedGrantsUri =
                UriFactory.CreateDocumentCollectionUri(databaseId, Constants.CollectionNames.PersistedGrant);
            Logger?.LogDebug($"Persisted Grants URI: {_persistedGrantsUri}");

            var partitionKeyDefinition = new PartitionKeyDefinition
                {Paths = {Constants.CollectionPartitionKeys.PersistedGrant}};
            Logger?.LogDebug($"Persisted Grants Partition Key: {partitionKeyDefinition}");

            var indexingPolicy = new IndexingPolicy
            {
                Automatic = true,
                IndexingMode = IndexingMode.Consistent,
                IncludedPaths =
                {
                    new IncludedPath
                    {
                        Path = "/expiration/?",
                        Indexes =
                        {
                            Index.Range(DataType.String)
                        }
                    },
                    new IncludedPath
                    {
                        Path = "/",
                        Indexes = {Index.Range(DataType.String)}
                    }
                }
            };
            Logger?.LogDebug($"Persisted Grants Indexing Policy: {indexingPolicy}");

            _persistedGrants = new DocumentCollection
            {
                Id = Constants.CollectionNames.PersistedGrant,
                PartitionKey = partitionKeyDefinition,
                IndexingPolicy = indexingPolicy
            };

            Logger?.LogDebug($"Persisted Grants Collection: {_persistedGrants}");

            var persistedGrantsRequestOptions = new RequestOptions
            {
                OfferThroughput = GetRUsFor(CollectionName.PersistedGrants)
            };
            Logger?.LogDebug($"Persisted Grants Request Options: {persistedGrantsRequestOptions}");

            Logger?.LogDebug($"Ensure Persisted Grants (ID:{_persistedGrants.Id}) collection exists...");
            var persistedGrantsResults =
                await DocumentClient.CreateDocumentCollectionIfNotExistsAsync(DatabaseUri, _persistedGrants,
                    persistedGrantsRequestOptions);
            Logger?.LogDebug($"{_persistedGrants.Id} Creation Results: {persistedGrantsResults.StatusCode}");
            if (persistedGrantsResults.StatusCode.EqualsOne(HttpStatusCode.Created, HttpStatusCode.OK))
                _persistedGrants = persistedGrantsResults.Resource;
        }
    }
}