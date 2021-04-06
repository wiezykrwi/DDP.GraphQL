using System;
using Sdl.Web.Delivery.DiscoveryService;

namespace DDP.GraphQL.ConsoleApp
{
    public class ServiceConfiguration : IDiscoveryServiceConfiguration
    {
        public int Timeout { get; } = 30;
        public bool SkipClaimCookieForwarding { get; }
        public bool IsOAuthEnabled { get; } = true;

        public Uri ServiceEndpoint { get; } = new Uri("https://udp-live-atlascopco-development.tridion.sdlproducts.com/discovery.svc");
        public Uri TokenServiceEndpoint { get; }
        public Uri IQServiceEndpoint { get; }

        public string ClientId { get; } = "cduser";
        public string ClientSecret { get; } = "";
    }
}