using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Linq;
using Newtonsoft.Json;
using System.Net;
using System.Security.Claims;

namespace MonkeyFinder
{
    public static class CosmosBroker
    {
        static readonly string PublicUser = "publicuser";

        [FunctionName("PublicBroker")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            [CosmosDB(databaseName: "zoo", collectionName: "monkeys", ConnectionStringSetting="CosmosConnectionString")] DocumentClient client,
            ILogger log)
        {
            var dbName = "zoo";
            var collectionName = "monkeys";

            Permission token = await GetPartitionPermission(PublicUser, client, dbName, collectionName, log);

            return new OkObjectResult(token.Token);            
        }

        [FunctionName("UserBroker")]
        public static async Task<IActionResult> UserBrokerRun(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            [CosmosDB(databaseName: "zoo", collectionName: "monkeys", ConnectionStringSetting="CosmosConnectionString")] DocumentClient client,
            ClaimsPrincipal claimsPrincipal,
            ILogger log)
        {
            if (claimsPrincipal == null)
                return new NotFoundResult();

            // Get the user id from the principal
            var azpClaim = claimsPrincipal.Claims.SingleOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");

            var userId = azpClaim?.Value;

            log.LogInformation($"UserId: {userId}");

            if (string.IsNullOrEmpty(userId))
                return new NotFoundResult();

            var dbName = "zoo";
            var collectionName = "monkeys";

            Permission token = await GetPartitionPermission(userId, client, dbName, collectionName, log);

            return new OkObjectResult(token.Token);                    
        }

        static async Task<Permission> GetPartitionPermission(string userId, DocumentClient client, string databaseId, string collectionId, ILogger log)
        {
            var permissionId = userId;
            Permission partitionPermission;

            Uri permissionUri = UriFactory.CreatePermissionUri(databaseId, userId, permissionId);
            Uri collectionUri = UriFactory.CreateDocumentCollectionUri(databaseId, collectionId);

            try
            {
                partitionPermission = await client.ReadPermissionAsync(permissionUri);
            
                log.LogInformation("Found partition permission");

                return partitionPermission;
            }
            catch (DocumentClientException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    await CreateUserIfNotExistAsync(userId, client, databaseId);
                }
            }

            Uri userUri = UriFactory.CreateUserUri(databaseId, userId);
            
            string partitionKeyValue;

            Permission newPermission = null;
            PermissionMode permissionMode = PermissionMode.Read;

            if (userId == PublicUser)
            {
                partitionKeyValue = PublicUser;
                permissionMode = PermissionMode.Read;                
            }
            else
            {
                partitionKeyValue = $"user-{userId}";
                permissionMode = PermissionMode.All;                
            }

            newPermission = new Permission
            {
                PermissionMode = permissionMode,
                Id = permissionId,
                ResourceLink = collectionUri.ToString(),
                ResourcePartitionKey = new PartitionKey(partitionKeyValue)
            };
            
            partitionPermission = await client.CreatePermissionAsync(userUri, newPermission);

            return partitionPermission;
        }        

        static async Task CreateUserIfNotExistAsync(string userId, DocumentClient client, string databaseId)
        {
            try
            {
                await client.ReadUserAsync(UriFactory.CreateUserUri(databaseId, userId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == HttpStatusCode.NotFound)
                {
                    await client.CreateUserAsync(UriFactory.CreateDatabaseUri(databaseId), new User { Id = userId });
                }
            }

        }
                
    }
}
