
using KubaPC.SharedClient.Services.Models;
using KubaPC.SharedClient.Services.Storage;
using Microsoft.AspNetCore.Components;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;

namespace KubaPC.SharedClient.Services.Http
{
    public class HttpService : IHttpService
    {
        private static readonly JsonSerializerOptions JSON_OPTIONS = new(JsonSerializerDefaults.Web);

        private const string PASSWORD_CHANGE_ERROR_CODE = "PasswordChangeRequired";
        private const string GENERAL_ERROR_KEY = "generalErrors";

        private readonly HttpClient httpClient;
        private readonly ITokenStore tokenStore;
        private readonly NavigationManager navigationManager;

        public HttpService(HttpClient httpClient, ITokenStore tokenStore, NavigationManager navigationManager)
        {
            this.httpClient = httpClient;
            this.tokenStore = tokenStore;
            this.navigationManager = navigationManager;
        }

        public async Task<ApiResult<T>> Get<T>(string uri)
        {
            return ToResult<T>(await Send(HttpMethod.Get, uri, null));
        }

        public async Task<ApiResult<T>> Post<T>(string uri, object body)
        {
            return ToResult<T>(await Send(HttpMethod.Post, uri, body));
        }

        public async Task<ApiResult> Post(string uri, object body)
        {
            return ToResult(await Send(HttpMethod.Post, uri, body));
        }

        public async Task<ApiResult> Put(string uri, object body)
        {
            return ToResult(await Send(HttpMethod.Put, uri, body));
        }

        private async Task<Outcome> Send(HttpMethod method, string uri, object? body)
        {
            using HttpRequestMessage request = new(method, uri);

            string? token = await tokenStore.GetToken();
            bool isTokenAttached = !string.IsNullOrWhiteSpace(token);

            if (isTokenAttached)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            if (body is not null)
            {
                string json = JsonSerializer.Serialize(body, JSON_OPTIONS);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            HttpResponseMessage response;

            try
            {
                response = await httpClient.SendAsync(request);
            }
            catch (Exception)
            {
                // No response at all: the API is down, the address is wrong, or the
                // browser blocked the call. Status 0 says "ne
                return new Outcome(false, 0, null, null, "The server could not be reached. Check your connection and try again.", EMPTY_ERRORS);
            }

            using (response)
            {
                string content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return new Outcome(true, (int)response.StatusCode, content, null, null, EMPTY_ERRORS);
                }

                return await HandleFailure(response.StatusCode, content, isTokenAttached);
            }
        }

        private async Task<Outcome> HandleFailure(HttpStatusCode statusCode, string content, bool isTokenAttached)
        {
            ErrorPayload payload = ReadErrorPayload(content);

            // A 401 while holding a token means that token is no longer good.
            // A 401 without one is simply a rejected sign-in
            if (statusCode == HttpStatusCode.Unauthorized && isTokenAttached)
            {
                await tokenStore.ClearToken();
                navigationManager.NavigateTo(ClientRoutes.LOGIN);
            }
            else if (statusCode == HttpStatusCode.Forbidden && payload.ErrorCode == PASSWORD_CHANGE_ERROR_CODE)
            {
                navigationManager.NavigateTo(ClientRoutes.CHANGE_PASSWORD);
            }

            return new Outcome(
                false,
                (int)statusCode,
                null,
                payload.ErrorCode,
                DescribeFailure(statusCode, payload, isTokenAttached),
                ReadValidationErrors(payload));
        }

        private static string DescribeFailure(HttpStatusCode statusCode, ErrorPayload payload, bool isTokenAttached)
        {
            string? generalError = ReadGeneralError(payload);

            return statusCode switch
            {
                HttpStatusCode.BadRequest => generalError ?? "Please correct the highlighted fields.",
                HttpStatusCode.Unauthorized when isTokenAttached => "Your session has ended. Please sign in again.",
                HttpStatusCode.Unauthorized => "The email address or password is incorrect.",
                HttpStatusCode.Forbidden => payload.Message ?? "You do not have permission to do that.",
                HttpStatusCode.NotFound => "That item no longer exists.",
                HttpStatusCode.Conflict => generalError ?? payload.Message ?? "That change conflicts with the current data.",
                _ => "Something went wrong. Please try again."
            };
        }

        private static ErrorPayload ReadErrorPayload(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return new ErrorPayload();
            }

            // An error body is not guaranteed to be JSON: a proxy or the hosting
            // platform can return HTML. Falling back keeps th
            try
            {
                return JsonSerializer.Deserialize<ErrorPayload>(content, JSON_OPTIONS) ?? new ErrorPayload();
            }
            catch (JsonException)
            {
                return new ErrorPayload();
            }
        }

        private static IReadOnlyDictionary<string, string[]> ReadValidationErrors(ErrorPayload payload)
        {
            if (payload.Errors is null || payload.Errors.Count == 0)
            {
                return EMPTY_ERRORS;
            }

            return payload.Errors
                .Where(entry => entry.Key != GENERAL_ERROR_KEY)
                .ToDictionary(entry => entry.Key, entry => entry.Value);
        }

        private static string? ReadGeneralError(ErrorPayload payload)
        {
            if (payload.Errors is not null
                && payload.Errors.TryGetValue(GENERAL_ERROR_KEY, out string[]? messages)
                && messages.Length > 0)
            {
                return messages[0];
            }

            return null;
        }

        private static ApiResult<T> ToResult<T>(Outcome outcome)
        {
            T? data = default;

            if (outcome.IsSuccess && !string.IsNullOrWhiteSpace(outcome.Content))
            {
                try
                {
                    data = JsonSerializer.Deserialize<T>(outcome.Content, JSON_OPTIONS);
                }
                catch (JsonException)
                {
                    return new ApiResult<T>
                    {
                        IsSuccess = false,
                        StatusCode = outcome.StatusCode,
                        ErrorMessage = "The server's response could not be read."
                    };
                }
            }

            return new ApiResult<T>
            {
                IsSuccess = outcome.IsSuccess,
                StatusCode = outcome.StatusCode,
                ErrorCode = outcome.ErrorCode,
                ErrorMessage = outcome.ErrorMessage,
                ValidationErrors = outcome.ValidationErrors,
                Data = data
            };
        }

        private static ApiResult ToResult(Outcome outcome)
        {
            return new ApiResult
            {
                IsSuccess = outcome.IsSuccess,
                StatusCode = outcome.StatusCode,
                ErrorCode = outcome.ErrorCode,
                ErrorMessage = outcome.ErrorMessage,
                ValidationErrors = outcome.ValidationErrors
            };
        }

        private static readonly IReadOnlyDictionary<string, string[]> EMPTY_ERRORS = new Dictionary<string, string[]>();

        /// <summary>What one HTTP exchange produced, before it is shaped for a caller.</summary>
        private sealed record Outcome(
            bool IsSuccess,
            int StatusCode,
            string? Content,
            string? ErrorCode,
            string? ErrorMessage,
            IReadOnlyDictionary<string, string[]> ValidationErrors);

        /// <summary>The API's error body. Covers both the valefusal shape.</summary>
        private sealed class ErrorPayload
        {
            public string? Message { get; set; }
            public string? ErrorCode { get; set; }
            public Dictionary<string, string[]>? Errors { get; set; }
        }
    }
}