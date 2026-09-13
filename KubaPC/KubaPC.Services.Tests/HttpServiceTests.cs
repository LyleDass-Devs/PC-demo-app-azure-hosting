using System.Net;
using System.Text;

using KubaPC.Services.Tests.TestDoubles;
using KubaPC.SharedClient.Services;
using KubaPC.SharedClient.Services.Http;
using KubaPC.SharedClient.Services.Models;

namespace KubaPC.Services.Tests
{
    public class HttpServiceTests
    {
        private sealed record Harness(
            HttpService Service,
            TestTokenStore TokenStore,
            TestNavigationManager Navigation,
            StubHttpMessageHandler Handler);

        private static Harness Build(HttpStatusCode statusCode, string body, string? storedToken = null)
        {
            return Build(_ => new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            }, storedToken);
        }

        private static Harness Build(Func<HttpRequestMessage, HttpResponseMessage> respond, string? storedToken = null)
        {
            StubHttpMessageHandler handler = new(respond);
            HttpClient httpClient = new(handler) { BaseAddress = new Uri("http://localhost:5161") };
            TestTokenStore tokenStore = new(storedToken);
            TestNavigationManager navigation = new();

            return new Harness(new HttpService(httpClient, tokenStore, navigation, new RequestActivity()), tokenStore, navigation, handler);
        }

        private sealed class Person
        {
            public string FullName { get; set; } = string.Empty;
        }

        // ---------- success ----------

        [Fact]
        public async Task Get_Success_ReturnsDeserialisedData()
        {
            Harness harness = Build(HttpStatusCode.OK, "{\"fullName\":\"Lyle Dass\"}");

            ApiResult<Person> result = await harness.Service.Get<Person>("/accounts/current");

            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.Equal("Lyle Dass", result.Data!.FullName);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public async Task Get_ResponseUsesCamelCase_BindsToPascalCaseProperty()
        {
            // The same class of silent mapping failure that bit the database layer.
            Harness harness = Build(HttpStatusCode.OK, "{\"fullName\":\"Case Check\"}");

            ApiResult<Person> result = await harness.Service.Get<Person>("/accounts/current");

            Assert.Equal("Case Check", result.Data!.FullName);
        }

        [Fact]
        public async Task Get_StoredToken_IsSentAsBearer()
        {
            Harness harness = Build(HttpStatusCode.OK, "{}", storedToken: "stored-token");

            await harness.Service.Get<Person>("/accounts/current");

            Assert.Equal("Bearer", harness.Handler.LastRequest!.Headers.Authorization!.Scheme);
            Assert.Equal("stored-token", harness.Handler.LastRequest.Headers.Authorization.Parameter);
        }

        [Fact]
        public async Task Get_NoStoredToken_SendsNoAuthorizationHeader()
        {
            Harness harness = Build(HttpStatusCode.OK, "{}");

            await harness.Service.Get<Person>("/accounts/current");

            Assert.Null(harness.Handler.LastRequest!.Headers.Authorization);
        }

        // ---------- validation ----------

        [Fact]
        public async Task Post_ValidationFailure_ExposesErrorsByField()
        {
            const string body = "{\"statusCode\":400,\"message\":\"One or more errors occurred!\","
                              + "\"errors\":{\"emailAddress\":[\"Email is required\"],\"password\":[\"Too short\"]}}";

            Harness harness = Build(HttpStatusCode.BadRequest, body);

            ApiResult result = await harness.Service.Post("/accounts/create", new { });

            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
            Assert.True(result.HasValidationErrors);
            Assert.Equal("Email is required", result.ValidationErrors["emailAddress"].Single());
            Assert.Equal("Too short", result.ValidationErrors["password"].Single());
        }

        [Fact]
        public async Task Post_GeneralError_IsReportedAsMessageNotAsAField()
        {
            // A refusal that belongs to no input must not be bound to one.
            const string body = "{\"statusCode\":409,\"message\":\"One or more errors occurred!\","
                              + "\"errors\":{\"generalErrors\":[\"An active account already exists for that address.\"]}}";

            Harness harness = Build(HttpStatusCode.Conflict, body);

            ApiResult result = await harness.Service.Post("/accounts/create", new { });

            Assert.False(result.IsSuccess);
            Assert.Equal(409, result.StatusCode);
            Assert.False(result.HasValidationErrors);
            Assert.Equal("An active account already exists for that address.", result.ErrorMessage);
        }

        // ---------- unauthorised ----------

        [Fact]
        public async Task Get_Unauthorised_WithToken_ClearsTokenAndGoesToLogin()
        {
            Harness harness = Build(HttpStatusCode.Unauthorized, "", storedToken: "expired-token");

            ApiResult<Person> result = await harness.Service.Get<Person>("/accounts/current");

            Assert.False(result.IsSuccess);
            Assert.True(harness.TokenStore.WasCleared);
            Assert.Equal(ClientRoutes.LOGIN, harness.Navigation.NavigatedTo);
            Assert.Contains("session", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Post_Unauthorised_WithoutToken_IsARejectedSignInNotAnExpiredSession()
        {
            // A failed login must leave the user on the form with its own message.
            Harness harness = Build(HttpStatusCode.Unauthorized, "");

            ApiResult result = await harness.Service.Post("/authentication/authenticate", new { });

            Assert.False(result.IsSuccess);
            Assert.False(harness.TokenStore.WasCleared);
            Assert.Null(harness.Navigation.NavigatedTo);
            Assert.Contains("password", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
        }

        // ---------- forbidden ----------

        [Fact]
        public async Task Get_PasswordChangeRequired_GoesToChangePassword()
        {
            const string body = "{\"errorCode\":\"PasswordChangeRequired\",\"message\":\"Password change required before continuing.\"}";

            Harness harness = Build(HttpStatusCode.Forbidden, body, storedToken: "must-change-token");

            ApiResult<Person> result = await harness.Service.Get<Person>("/accounts/current");

            Assert.Equal("PasswordChangeRequired", result.ErrorCode);
            Assert.Equal(ClientRoutes.CHANGE_PASSWORD, harness.Navigation.NavigatedTo);
            Assert.False(harness.TokenStore.WasCleared);
        }

        [Fact]
        public async Task Get_ForbiddenByRole_StaysOnThePage()
        {
            Harness harness = Build(HttpStatusCode.Forbidden, "", storedToken: "monitor-token");

            ApiResult<Person> result = await harness.Service.Get<Person>("/accounts");

            Assert.False(result.IsSuccess);
            Assert.Null(result.ErrorCode);
            Assert.Null(harness.Navigation.NavigatedTo);
            Assert.Contains("permission", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
        }

        // ---------- failures that are not the API's fault ----------

        [Fact]
        public async Task Get_ServerUnreachable_ReportsStatusZero()
        {
            Harness harness = Build(_ => throw new HttpRequestException("no route to host"));

            ApiResult<Person> result = await harness.Service.Get<Person>("/accounts/current");

            Assert.False(result.IsSuccess);
            Assert.Equal(0, result.StatusCode);
            Assert.Contains("could not be reached", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Get_ErrorBodyIsNotJson_KeepsTheStatusCode()
        {
            // A proxy or load balancer can answer with HTML.
            Harness harness = Build(HttpStatusCode.BadGateway, "<html><body>502 Bad Gateway</body></html>");

            ApiResult<Person> result = await harness.Service.Get<Person>("/accounts/current");

            Assert.False(result.IsSuccess);
            Assert.Equal(502, result.StatusCode);
            Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }

        [Fact]
        public async Task Get_SuccessBodyIsNotJson_FailsWithoutThrowing()
        {
            Harness harness = Build(HttpStatusCode.OK, "not json at all");

            ApiResult<Person> result = await harness.Service.Get<Person>("/accounts/current");

            Assert.False(result.IsSuccess);
            Assert.Contains("could not be read", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
        }

        // ---------- verbs ----------

        [Fact]
        public async Task Put_SendsPutWithJsonBody()
        {
            Harness harness = Build(HttpStatusCode.OK, "{}");

            await harness.Service.Put("/accounts/update", new { fullName = "Changed" });

            HttpRequestMessage request = harness.Handler.LastRequest!;

            Assert.Equal(HttpMethod.Put, request.Method);
            Assert.Contains("\"fullName\":\"Changed\"", harness.Handler.LastRequestBody!);
        }
    }
}
