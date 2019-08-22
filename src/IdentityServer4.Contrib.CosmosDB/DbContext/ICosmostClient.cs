using Microsoft.Azure.Documents.Client;

namespace IdentityServer4.Contrib.CosmosDB.DbContext
{
    public interface ICosmostClient
    {
        DocumentClient DocumentClient { get; }
    }
}