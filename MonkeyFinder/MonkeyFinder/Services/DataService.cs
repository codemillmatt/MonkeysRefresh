using System;
using System.Collections.Generic;
using MonkeyFinder.Model;

using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Http;
using Microsoft.Identity.Client;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace MonkeyFinder
{
    public enum DocumentType
    {
        Public,
        User
    }

    public class DataService
    {
        static readonly string PublicDocuments = "publicuser";
        static readonly string UserDocuments = "user";
        
        static readonly string databaseName = "zoo";
        static readonly string collectionName = "monkeys";

        async Task<DocumentClient> Initialize(DocumentType documentType)
        {            
            try
            {
                var accessToken = await GetAccessToken(documentType);

                if (string.IsNullOrEmpty(accessToken))
                    return null;

                var docClient = new DocumentClient(new Uri(APIKeys.CosmosEndpointUrl), accessToken);

                return docClient;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                
                return null;
            }            
        }

        public async Task<List<T>> GetAllItems<T>(DocumentType documentType)
        {
            var allItems = new List<T>();

            try
            {
                DocumentClient docClient = await Initialize(documentType);

                if (docClient == null)
                    return allItems;

                string partition;
                PartitionKey partitionKey = null;

                if (documentType == DocumentType.Public)
                {
                    partition = PublicDocuments;
                    partitionKey = new PartitionKey(PublicDocuments);
                }
                else
                {
                    if (AuthenticationService.Instance.CurrentUser == null || !AuthenticationService.Instance.CurrentUser.IsLoggedOn)
                        throw new Exception("User must be logged in");

                    partition = $"{UserDocuments}-{AuthenticationService.Instance.CurrentUser.UserIdentifier}";

                    partitionKey = new PartitionKey(partition);
                }

                var genericQuery = docClient.CreateDocumentQuery<EmptyDocument<T>>(
                    UriFactory.CreateDocumentCollectionUri(databaseName, collectionName),
                    new FeedOptions { MaxItemCount = -1, PartitionKey = partitionKey })
                    .AsDocumentQuery();

                while (genericQuery.HasMoreResults)
                {
                    var genericResults = await genericQuery.ExecuteNextAsync<EmptyDocument<T>>();

                    allItems.AddRange(genericResults.Select(m => m.Document));
                }                
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }

            return allItems;
        }

        public async Task SaveItem<T>(string id, T itemToSave)
        {
            if (AuthenticationService.Instance.CurrentUser == null ||
                !AuthenticationService.Instance.CurrentUser.IsLoggedOn)
            {
                return;
            }

            try
            {
                DocumentClient docClient = await Initialize(DocumentType.User);

                if (docClient == null)
                    return;

                var docToSave = new EmptyDocument<T>();
                docToSave.Id = id;
                docToSave.Document = itemToSave;
                docToSave.PartitionKey = $"user-{AuthenticationService.Instance.CurrentUser.UserIdentifier}";

                var collectionUri = UriFactory.CreateDocumentCollectionUri(databaseName, collectionName);
                await docClient.CreateDocumentAsync(collectionUri, docToSave);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        public async Task<string> GetAccessToken(DocumentType documentType)
        {
            try
            {
                var baseUri = new Uri(APIKeys.TokenEndpointBaseUrl);
                var httpClient = new HttpClient { BaseAddress = baseUri };
                HttpRequestMessage message;

                if (documentType == DocumentType.Public)
                {
                    message = new HttpRequestMessage(HttpMethod.Get,
                        new Uri(baseUri, APIKeys.PublicTokenPath));
                }
                else
                {
                    message = new HttpRequestMessage(HttpMethod.Get,
                        new Uri(baseUri, APIKeys.UserTokenPath));

                    message.Headers.Authorization = new AuthenticationHeaderValue("Bearer",
                        AuthenticationService.Instance.CurrentUser.AccessToken);
                }

                var tokenResponse = await httpClient.SendAsync(message);
                tokenResponse.EnsureSuccessStatusCode();

                var token = await tokenResponse.Content.ReadAsStringAsync();

                return token;                
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                return "";
            }
        }

    }
}