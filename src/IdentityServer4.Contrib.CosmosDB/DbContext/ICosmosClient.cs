using Microsoft.Azure.Documents.Client;

namespace IdentityServer4.Contrib.CosmosDB.DbContext
{
    public interface ICosmosClient
    {
        DocumentClient DocumentClient { get; }
    }
}