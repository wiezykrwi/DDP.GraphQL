using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sdl.Tridion.Api.Client;
using Sdl.Tridion.Api.Client.ContentModel;
using Sdl.Tridion.Api.GraphQL.Client;
using Sdl.Web.Delivery.DiscoveryService.Tridion.WebDelivery.Configuration;
using Sdl.Web.Delivery.DiscoveryService.Tridion.WebDelivery.Platform;

namespace DDP.GraphQL.ConsoleApp
{
    public static class Program
    {
        public static async Task Main()
        {
            var config = new ServiceConfiguration();

            var tridionWebDiscovery = new TridionWebDiscovery(config.ServiceEndpoint);
            var tokenService = tridionWebDiscovery.CreateQuery<TokenServiceCapability>("TokenServiceCapabilities").First();

            var oAuthTokenProvider = new OAuthTokenProvider(new Uri(tokenService.URI), config.ClientId, config.ClientSecret, null);
            var auth = new OAuth(oAuthTokenProvider);

            tridionWebDiscovery.BuildingRequest += (sender, args) =>
            {
                args.Headers.Add(oAuthTokenProvider.AuthRequestHeaderName,
                    oAuthTokenProvider.AuthRequestHeaderValue);
            };

            var contentService = tridionWebDiscovery.CreateQuery<ContentServiceCapability>("ContentServiceCapabilities").First();

            var contentEndpoint = contentService.URI.Replace("content.svc", "cd/api");

            var graphQlClient = new GraphQLClient(contentEndpoint, auth);
            var apiClient = new ApiClient(graphQlClient);

            var inputItemFilter = new InputItemFilter
            {
                ItemTypes = new List<FilterItemType> {FilterItemType.PUBLICATION},
                CustomMeta = new InputCustomMetaCriteria()
                {
                    Key = "ishref.object.value",
                    Value = "GUID-775F0382-4F41-4EA4-A43B-39BE89669506"
                }
            };

            var publicationNodes = await apiClient.ExecuteItemQueryAsync(inputItemFilter, null, new Pagination
            {
                First = 50
            }, null, ContentIncludeMode.Exclude, false, null);

            var publicationMeta = publicationNodes.Edges[0].Node;

            var sitemap = await apiClient.GetSitemapSubtreeAsync(
                publicationMeta.NamespaceId,
                publicationMeta.PublicationId,
                "t1",
                0,
                Ancestor.NONE,
                null);

            Console.WriteLine(sitemap.Count);

            Console.WriteLine("Press the any key...");
            Console.ReadLine();
        }
    }
}
