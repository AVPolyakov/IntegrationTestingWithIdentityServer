using IdentityModel.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using MyApiServer;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MyApiServerTests
{
    public class UnitTest1
    {
        private readonly HttpClient _httpClient;
        private readonly TokenClient _tokenClient;

        public UnitTest1()
        {
            //https://stackoverflow.com/a/40559393
            var idBuilder = new WebHostBuilder();
            idBuilder.UseStartup<MyIdentityServer.Startup>();
            //...

            TestServer identityTestServer = new TestServer(idBuilder);


            var httpMessageHandler = identityTestServer.CreateHandler();

            const string authority = "http://localhost:5001";
            var webhost = new WebHostBuilder()
            .UseUrls("http://*:8000")
            .ConfigureServices(collection => collection.AddTransient(
                provider => new JwtBearerSettings{Action = options => {
                    options.Authority = authority;
                    options.Audience = $"{authority}/resources";

                    // IMPORTANT PART HERE
                    options.BackchannelHttpHandler = httpMessageHandler;
                    //IntrospectionDiscoveryHandler = identityTestServer.CreateHandler(),
                    //IntrospectionBackChannelHandler = identityTestServer.CreateHandler()    
                    options.RequireHttpsMetadata = false;
                }}))
            .UseStartup<Startup>();

            var server = new TestServer(webhost);
            _httpClient = server.CreateClient();

            var disco = new DiscoveryClient(authority, httpMessageHandler).GetAsync().Result;
            _tokenClient = new TokenClient(disco.TokenEndpoint, "client", "secret", httpMessageHandler);
        }

        [Fact]
        public async void ShouldNotAllowAnonymousUser()
        {
            var result = await _httpClient.GetAsync("http://localhost:8000/api/values");
            Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
        }

        [Fact]
        public async void ShouldReturnValuesForAuthenticatedUser()
        {
            var tokenResponse = _tokenClient.RequestResourceOwnerPasswordAsync("alice", "password", "api1").Result;
            _httpClient.SetBearerToken(tokenResponse.AccessToken);

            var result = await _httpClient.GetStringAsync("http://localhost:8000/api/values");
            Assert.Equal("[\"value1\",\"value2\"]", result);
        }
    }
}