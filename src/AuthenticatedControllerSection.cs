using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
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
                        throw new UnauthenticatedExpection("access token not available at this time");

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

            if (IsAnonymous(request.Method, path))
                return false;

            return true;           
        }

        protected virtual bool IsAnonymous(HttpMethod method, string path)
        {
            var normalizedPath = NormalizePath(path);

            if (normalizedPath == "/health")
                return true;

            var methodRules = AnonymousPathsByMethod;
            if (methodRules != null)
            {
                foreach (var rule in methodRules)
                {
                    if (!PathEquals(rule.Key, normalizedPath))
                        continue;

                    var methods = rule.Value;
                    if (methods == null || methods.Length == 0)
                        return false;

                    var requestMethod = method.Method;
                    if (methods.Any(allowed => string.Equals(allowed, "*", StringComparison.OrdinalIgnoreCase) || string.Equals(allowed, requestMethod, StringComparison.OrdinalIgnoreCase)))
                        return true;

                    return false;
                }
            }

            return IsAnonymous(normalizedPath);
        }

        protected virtual bool IsAnonymous(string path)
        {
            var normalizedPath = NormalizePath(path);

            if (normalizedPath == "/health") return true;
            if (AnonymousPaths != null && AnonymousPaths.Any(candidate => PathEquals(candidate, normalizedPath))) return true;

            return false;
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "/";

            var normalized = path.Trim();
            if (!normalized.StartsWith("/"))
                normalized = "/" + normalized;

            return normalized;
        }

        private static bool PathEquals(string? left, string? right)
            => string.Equals(NormalizePath(left ?? string.Empty), NormalizePath(right ?? string.Empty), StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Paths considered anonymous for specific HTTP methods.
        /// </summary>
        /// <remarks>
        /// These rules have precedence over <see cref="AnonymousPaths"/> for the same path.
        /// Key = path, Value = allowed HTTP methods (e.g. GET, POST). Use "*" to allow all methods for that path.
        /// </remarks>
        protected virtual IReadOnlyDictionary<string, string[]>? AnonymousPathsByMethod { get; }

        /// <summary>
        /// Paths that are considered anonymous, and do not require authentication.
        /// </summary>
        /// <remarks>
        /// Only use that if you are sure that the path is anonymous and does not require authentication, for all requests methods (GET, POST, PUT, DELETE, etc.).
        /// Otherwise, you will miss the authentication for those requests.
        /// </remarks>
        protected virtual string[]? AnonymousPaths { get; }
    }
}
