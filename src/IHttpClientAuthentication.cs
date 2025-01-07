using Sufficit.Identity;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Sufficit.Net.Http
{
    public interface IHttpClientAuthentication
    {
        ITokenProvider TokenProvider { get; }

        void Configure(IHttpClientOptions options);

        ValueTask Authenticate(HttpRequestMessage request, CancellationToken cancellationToken);

        bool ShouldAuthenticate(HttpRequestMessage request);
    }
}
