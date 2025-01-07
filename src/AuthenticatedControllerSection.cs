using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using Sufficit.Identity;

namespace Sufficit.Net.Http
{
    public abstract class AuthenticatedControllerSection : ControllerSection
    {
        private readonly ITokenProvider _tokens;

        public AuthenticatedControllerSection (IAuthenticatedControllerBase cb) : this (cb, cb.Tokens) { }

        public AuthenticatedControllerSection (IControllerBase cb, ITokenProvider tokens) : base (cb) 
        {
            _tokens = tokens;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // trying to authenticate request, if necessary
            await Authenticate(request, cancellationToken);

            // Proceed calling the inner handler, that will actually send the request
            // to our protected api
            return await base.SendAsync(request, cancellationToken);
        }

        protected virtual async ValueTask Authenticate(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (ShouldAuthenticate(request))
            {
                if (request.Headers.Authorization == null)
                {
                    // request the access token
                    var accessToken = await _tokens.GetTokenAsync();

                    if (string.IsNullOrWhiteSpace(accessToken))
                        throw new UnauthorizedAccessException("access token not available at this time");

                    // set the bearer token to the outgoing request
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                }
            }
        }

        protected virtual bool ShouldAuthenticate (HttpRequestMessage request)
        {
            if (request.Method == HttpMethod.Head)
                return false;

            if (request.RequestUri == null)
                return false;

            string path;
            if (request.RequestUri.IsAbsoluteUri)
                path = request.RequestUri.AbsolutePath;
            else
                path = request.RequestUri.ToString().Split('?').First();

            if (IsAnonymous(path))
                return false;

            return true;           
        }

        protected virtual bool IsAnonymous(string path)
        {
            if (path == "/health") return true;
            if (AnonymousPaths != null && AnonymousPaths.Contains(path)) return true;

            return false;
        }

        protected virtual string[]? AnonymousPaths { get; }
    }
}
