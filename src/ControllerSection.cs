using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Sufficit.Net.Http
{
    public abstract class ControllerSection
    {
        private readonly Action<bool>? _healthy;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _json;

        public ControllerSection (HttpClient client, JsonSerializerOptions json)
        {
            _client = client;
            _json = json;
        }

        public ControllerSection (IControllerBase cb) : this(cb.Client, cb.Json)
        {
            _healthy = cb.Healthy;
        }

        protected Task<IEnumerable<T>> RequestManyStruct<T>(HttpRequestMessage message, CancellationToken cancellationToken)
            => RequestManyInternal<T>(message, cancellationToken);

        protected Task<IEnumerable<T>> RequestMany<T>(HttpRequestMessage message, CancellationToken cancellationToken) where T : class, new()
            => RequestManyInternal<T>(message, cancellationToken);

        protected IAsyncEnumerable<T> RequestManyAsAsyncEnumerable<T>(HttpRequestMessage message, CancellationToken cancellationToken) where T : class, new()
            => RequestManyInternalAsAsyncEnumerable<T>(message, cancellationToken);

        private async Task<IEnumerable<T>> RequestManyInternal<T>(HttpRequestMessage message, CancellationToken cancellationToken)
        {
            using var response = await SendAsync(message, cancellationToken);
            await response.EnsureSuccess(cancellationToken);

            // updating healthy for this controller
            _healthy?.Invoke(true);

            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                return Enumerable.Empty<T>();

            return await response.Content.ReadFromJsonAsync<IEnumerable<T>>(_json, cancellationToken) ?? Enumerable.Empty<T>();
        }

        private async IAsyncEnumerable<T> RequestManyInternalAsAsyncEnumerable<T>(HttpRequestMessage message, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using var response = await SendAsync(message, cancellationToken);
            await response.EnsureSuccess(cancellationToken);

            // updating healthy for this controller
            _healthy?.Invoke(true);

            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                yield break;

            await foreach (var item in response.Content.ReadFromJsonAsAsyncEnumerable<T>(_json, cancellationToken))
                if (item != null) yield return item;
        }

        protected async Task<T?> RequestStruct<T>(HttpRequestMessage message, CancellationToken cancellationToken) where T : struct
        {
            using var response = await SendAsync(message, cancellationToken);
            await response.EnsureSuccess(cancellationToken);

            // updating healthy for this controller
            _healthy?.Invoke(true);

            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                return null;

            return await response.Content.ReadFromJsonAsync<T>(_json, cancellationToken);
        }

        protected async Task<T?> Request<T> (HttpRequestMessage message, CancellationToken cancellationToken) where T : class, new()
        {
            using var response = await SendAsync(message, cancellationToken);
            await response.EnsureSuccess(cancellationToken);

            // updating healthy for this controller
            _healthy?.Invoke(true);

            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                return null;

            return await response.Content.ReadFromJsonAsync<T>(_json, cancellationToken);
        }

        protected async Task<byte[]?> RequestBytes (HttpRequestMessage message, CancellationToken cancellationToken)
        {
            using var response = await SendAsync(message, cancellationToken);
            await response.EnsureSuccess(cancellationToken);

            // updating healthy for this controller
            _healthy?.Invoke(true);

            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                return null;

#if NETSTANDARD
            return await response.Content.ReadAsByteArrayAsync();
#else
            return await response.Content.ReadAsByteArrayAsync(cancellationToken);
#endif
        }

        protected async Task Request (HttpRequestMessage message, CancellationToken cancellationToken)
        {
            using var response = await SendAsync(message, cancellationToken);
            await response.EnsureSuccess(cancellationToken);

            // updating healthy for this controller
            _healthy?.Invoke(true);
        }

        protected virtual Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => _client.SendAsync(request, cancellationToken);
    }
}
