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
    ///     Configuration DbContext Class.
    /// </summary>
    public class ConfigurationDbContext : CosmosDbContextBase, IConfigurationDbContext
    {
        private DocumentCollection _configurationsResources;
        private Uri _configurationsResourcesUri;
        private string databaseId;
        /// <summary>
        ///     Create an instance of the ConfigurationDbContext Class.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="databaseName"></param>
        /// <param name="connectionPolicy"></param>
        /// <param name="logger"></param>
        public ConfigurationDbContext(IOptions<CosmosDbConfiguration> settings,
                                      ICosmosClient cosmosClient,
            string databaseName = Constants.DatabaseName,
            ConnectionPolicy connectionPolicy = null,
            ILogger<ConfigurationDbContext> logger = null)
            : base(settings, cosmosClient, logger)
        {
            Guard.ForNullOrDefault(settings.Value, nameof(settings));

            databaseId = Configuration.DatabaseName.GetValueOrDefault(databaseName);

            _configurationsResourcesUri =
                UriFactory.CreateDocumentCollectionUri(databaseId, Constants.CollectionNames.Configurations);
            Logger?.LogDebug($"API Resources URI: {_configurationsResourcesUri}");

            var partitionKeyDefinition = new PartitionKeyDefinition
                { Paths = { Constants.CollectionPartitionKeys.Global } };
            Logger?.LogDebug($"API Resources Partition Key: {partitionKeyDefinition}");

            var indexingPolicy = new IndexingPolicy
            {
                Automatic = true,
                IndexingMode = IndexingMode.Consistent
            };
            Logger?.LogDebug($"API Resources Index Policy: {indexingPolicy}");

            _configurationsResources = new DocumentCollection
            {
                Id = Constants.CollectionNames.Configurations,
                PartitionKey = partitionKeyDefinition,
                IndexingPolicy = indexingPolicy
            };
        }

        /// <summary>
        /// 
        /// </summary>
        public async Task AddDocument<T>(T entity)
        {
            await DocumentClient.CreateDocumentAsync(_configurationsResourcesUri, entity);
        }

        /// <summary>
        ///     Generic call to get document from cosmos db
        /// </summary>
        public async Task<IEnumerable<T>> GetDocument<T>(Expression<Func<T, bool>> predicate = null)
        {
            var feedOptions = new FeedOptions
            {
                PartitionKey = new PartitionKey(typeof(T).Name)
            };

            IDocumentQuery<T> query;
            if (predicate == null)
            {
                 query = DocumentClient.CreateDocumentQuery<T>(_configurationsResourcesUri, feedOptions)
                    .AsDocumentQuery();
            }
            else
            {
                query = DocumentClient.CreateDocumentQuery<T>(_configurationsResourcesUri, feedOptions)
                    .Where(predicate)
                    .AsDocumentQuery();
            }
            
            var results = new List<T>();
            while (query.HasMoreResults)
            {
                FeedResponse<T> res = await query.ExecuteNextAsync<T>();
                results.AddRange(res);
            }

            return results;
        }

        public async Task EnsureConfigurationsCollectionCreated()
        {
            EnsureDatabaseCreated(Constants.DatabaseName);

            _configurationsResourcesUri =
                UriFactory.CreateDocumentCollectionUri(databaseId, Constants.CollectionNames.Configurations);
            Logger?.LogDebug($"API Resources URI: {_configurationsResourcesUri}");

            var partitionKeyDefinition = new PartitionKeyDefinition
                { Paths = { Constants.CollectionPartitionKeys.Global } };
            Logger?.LogDebug($"API Resources Partition Key: {partitionKeyDefinition}");

            var indexingPolicy = new IndexingPolicy
            {
                Automatic = true,
                IndexingMode = IndexingMode.Consistent
            };
            Logger?.LogDebug($"API Resources Index Policy: {indexingPolicy}");

            _configurationsResources = new DocumentCollection
            {
                Id = Constants.CollectionNames.Configurations,
                PartitionKey = partitionKeyDefinition,
                IndexingPolicy = indexingPolicy
            };
            Logger?.LogDebug($"API Resources Collection: {_configurationsResources}");

            var apiResourcesRequestOptions = new RequestOptions
            {
                OfferThroughput = GetRUsFor(CollectionName.Global)
            };
            Logger?.LogDebug($"API Resources Request Options: {apiResourcesRequestOptions}");

            Logger?.LogDebug($"Ensure API Resources (ID:{_configurationsResources.Id}) collection exists...");
            ResourceResponse<DocumentCollection> _configurationsResourcesResults = await DocumentClient.CreateDocumentCollectionIfNotExistsAsync(DatabaseUri,
                                                                                                                                         _configurationsResources, apiResourcesRequestOptions);
            Logger?.LogDebug($"{_configurationsResources.Id} Creation Results: {_configurationsResourcesResults.StatusCode}");
            if (_configurationsResourcesResults.StatusCode.EqualsOne(HttpStatusCode.Created, HttpStatusCode.OK))
                _configurationsResources = _configurationsResourcesResults.Resource;
        }
    }
}