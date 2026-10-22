using KubaPC.SharedClient.Services.Storage;
using Microsoft.AspNetCore.Components;

namespace KubaPC.Services.Tests.TestDoubles
{

    public class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> respond;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        {
            this.respond = respond;
        }

        public HttpRequestMessage? LastRequest { get; private set; }

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
