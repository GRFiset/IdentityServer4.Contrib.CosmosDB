using System;
using System.Collections.Generic;
using System.Text;
using IdentityServer4.Contrib.CosmosDB.Configuration;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Options;

namespace IdentityServer4.Contrib.CosmosDB.DbContext
{
    public class CosmosClient : ICosmosClient
    {
        /// <summary>
        ///     CosmosDb Document Client.
        /// </summary>
        public DocumentClient DocumentClient { get; }

        public CosmosClient(IOptions<CosmosDbConfiguration> settings,
                            ConnectionPolicy connectionPolicy = null)
        {
            var serviceEndPoint = new Uri(settings.Value.EndPointUrl);

            DocumentClient = new DocumentClient(serviceEndPoint, settings.Value.PrimaryKey,
                                                connectionPolicy ?? ConnectionPolicy.Default);

            DocumentClient.OpenAsync();
        }
    }
}
