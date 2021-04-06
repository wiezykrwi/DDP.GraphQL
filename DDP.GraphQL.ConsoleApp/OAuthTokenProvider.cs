using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Security.Authentication;
using Sdl.Web.Delivery.Core;
using Sdl.Web.Delivery.OAuthClient;
using Sdl.Web.Delivery.Service;

namespace DDP.GraphQL.ConsoleApp
{
    public class OAuthTokenProvider : IOAuthTokenProvider
    {
        private readonly ConcurrentDictionary<string, IToken> _tokens = new ConcurrentDictionary<string, IToken>();
        private AuthenticationClient _client;
        private readonly string _clientResource;

        private Uri TokenServiceUri { get; }

        private string ClientId { get; }

        private string ClientSecret { get; }

        public OAuthTokenProvider(
            Uri uri,
            string clientId,
            string clientSecret,
            string clientResource)
        {
            TokenServiceUri = uri;
            ClientId = clientId;
            ClientSecret = clientSecret;
            _clientResource = clientResource;
        }

        public virtual IToken Token
        {
            get
            {
                try
                {
                    return TokenWithExceptions;
                }
                catch
                {
                    return null;
                }
            }
        }

        private IToken TokenWithExceptions
        {
            get
            {
                return _tokens.AddOrUpdate("TOKEN",
                    key => CreateOrRefreshToken(null),
                    (key, value) => CreateOrRefreshToken(value));
            }
        }

        public virtual bool Refresh()
        {
            IToken tokenWithExceptions = TokenWithExceptions;
            return tokenWithExceptions != null && !tokenWithExceptions.Expired;
        }

        public virtual string AuthRequestHeaderName => "authorization";

        public virtual string AuthRequestHeaderValue
        {
            get
            {
                IToken token = Token;
                return token == null ? null : $"Bearer {token.AccessToken}";
            }
        }

        protected virtual IToken CreateOrRefreshToken(IToken token)
        {
            if (token == null)
            {
                if (_client == null)
                    _client = new AuthenticationClient(TokenServiceUri);
                token = Authenticate(ClientId, ClientSecret, _clientResource);
            }

            if (token == null || !token.Expired) return token;

            if (token is RefreshableToken refreshableToken)
            {
                token = _client.RefreshToken(refreshableToken.RefreshToken, ClientId, _clientResource) ??
                        _client.Authenticate(ClientId, ClientSecret, _clientResource);
            }
            else
            {
                token = _client.Authenticate(ClientId, ClientSecret, _clientResource);
            }

            return token;
        }

        private IToken Authenticate(string clientId, string clientSecret, string resource = null)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                throw new ArgumentNullException(nameof (clientId));
            }

            if (string.IsNullOrEmpty(clientSecret))
            {
                throw new ArgumentNullException(nameof (clientSecret));
            }

            string parameters = $"grant_type=client_credentials&client_id={clientId}&client_secret={clientSecret}";
            if (!string.IsNullOrEmpty(resource))
            {
                parameters = parameters + "&resource=" + resource;
            }

            return this.ProcessRequest(parameters);
        }

        private IToken ProcessRequest(string parameters)
        {
            string str;
            try
            {
                str = HttpUtils.GetResponseString(TokenServiceUri, "POST", "application/x-www-form-urlencoded", parameters, null, null);
            }
            catch (WebException ex)
            {
                str = IOUtils.ReadStream(ex.Response.GetResponseStream());
            }

            IToken token = this.ConstructObjectFromJson(str);
            if (token != null)
                return token;
            throw new Exception(str);
        }

        private IToken ConstructObjectFromJson(string responseString)
        {
            var jsonObject = Json.Deserialize<JsonObject>(responseString, null);

            if (jsonObject.Get("access_token") == null)
                throw new Exception(responseString);

            IToken token = new Token();
            if (jsonObject.Get("refresh_token") != null)
                token = new RefreshableToken()
                {
                    RefreshToken = jsonObject.Get("refresh_token")
                };
            token.AccessToken = jsonObject.Get("access_token");
            if (jsonObject.Get("expires_in") != null)
                token.ExpiresAt = DateTime.UtcNow.AddSeconds(jsonObject.Get<int>("expires_in"));
            return token;
        }

    }
}