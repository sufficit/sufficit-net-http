using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Sufficit.Net.Http
{
    public static class HttpResponseMessageExtensions
    {
        /// <summary>
        ///     Nearly the HttpResponseMessage.EnsureSuccessStatusCode(), but reads the content from request before throws
        /// </summary>
        public static async ValueTask EnsureSuccess(this HttpResponseMessage response, CancellationToken cancellationToken = default)
        {
            if (!response.IsSuccessStatusCode)
            {
                cancellationToken.ThrowIfCancellationRequested();

#if NET5_0_OR_GREATER
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!string.IsNullOrWhiteSpace(content))
                    throw new HttpRequestException(
                        ExtractErrorMessage(content),
                        new Exception(response.ReasonPhrase),
                        response.StatusCode);
                else
                    response.EnsureSuccessStatusCode();
#else
                var content = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrWhiteSpace(content))
                    throw new HttpRequestException(ExtractErrorMessage(content));
                else
                    response.EnsureSuccessStatusCode();
#endif
            }
        }

        /// <summary>
        /// Returns the API message when the response follows the standard endpoint
        /// envelope ("message") or the RFC 7807 ProblemDetails shape ("detail",
        /// then "title"), preserving the original body for non-JSON responses.
        /// </summary>
        private static string ExtractErrorMessage(string content)
        {
            try
            {
                using var document = JsonDocument.Parse(content);
                if (document.RootElement.ValueKind == JsonValueKind.Object)
                {
                    // Canonical Sufficit envelope first.
                    if (document.RootElement.TryGetProperty("message", out var messageElement)
                        && messageElement.ValueKind == JsonValueKind.String)
                    {
                        var message = messageElement.GetString();
                        if (!string.IsNullOrWhiteSpace(message))
                            return message;
                    }

                    // RFC 7807 ProblemDetails used by typed conflict responses.
                    if (document.RootElement.TryGetProperty("detail", out var detailElement)
                        && detailElement.ValueKind == JsonValueKind.String)
                    {
                        var detail = detailElement.GetString();
                        if (!string.IsNullOrWhiteSpace(detail))
                            return detail;
                    }

                    if (document.RootElement.TryGetProperty("title", out var titleElement)
                        && titleElement.ValueKind == JsonValueKind.String)
                    {
                        var title = titleElement.GetString();
                        if (!string.IsNullOrWhiteSpace(title))
                            return title;
                    }
                }
            }
            catch (System.Text.Json.JsonException)
            {
            }

            return content;
        }
    }
}
