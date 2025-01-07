using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
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
                    throw new HttpRequestException(content, new Exception(response.ReasonPhrase), response.StatusCode);
                else
                    response.EnsureSuccessStatusCode();
#else
                var content = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrWhiteSpace(content))
                    throw new HttpRequestException(content);
                else
                    response.EnsureSuccessStatusCode();
#endif
            }
        }
    }
}
