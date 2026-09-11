using KubaPC.SharedClient.Services.Storage;
using Microsoft.AspNetCore.Components;

namespace KubaPC.Services.Tests.TestDoubles
{
    /// <summary>
    /// Returns a caller-supplied response without touching the network, and
    /// records the request so a test can assert what was sent.
    /// </summary>
    public class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> respond;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        {
            this.respond = respond;
        }

        public HttpRequestMessage? LastRequest { get; private set; }

        /// <summary>
        /// The request body, read here because the caller disposes the request as
        /// soon as the call returns.
        /// </summary>
        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;

            if (request.Content is not null)
            {
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            return respond(request);
        }
    }

    /// <summary>Holds a token in memory and records whether it was cleared.</summary>
    public class TestTokenStore : ITokenStore
    {
        public TestTokenStore(string? token = null)
        {
            Token = token;
        }

        public string? Token { get; private set; }

        public bool WasCleared { get; private set; }

        public Task<string?> GetToken() => Task.FromResult(Token);

        public Task SetToken(string token)
        {
            Token = token;
            return Task.CompletedTask;
        }

        public Task ClearToken()
        {
            Token = null;
            WasCleared = true;
            return Task.CompletedTask;
        }
    }

    /// <summary>Records where the application tried to navigate instead of navigating.</summary>
    public class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager()
        {
            Initialize("http://localhost:5055/", "http://localhost:5055/");
        }

        public string? NavigatedTo { get; private set; }

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            NavigatedTo = uri;
        }

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
            NavigatedTo = uri;
        }
    }
}
