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
    /// <summary>
    /// Base controller section that can attach an access token (Bearer) to outgoing HTTP requests.
    /// </summary>
    /// <remarks>
    /// Default behavior:
    /// - If an access token is available, it will be attached to the request.
    /// - If no token is available, the request is only allowed to proceed when it targets an anonymous path.
    /// - <see cref="HttpMethod.Head"/> is never authenticated (token is not attached).
    /// - <see cref="HttpRequestMessage.RequestUri"/> is required to evaluate anonymous paths.
    /// </remarks>
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

        /// <summary>
        /// Tries to attach an Authorization header (Bearer token) to the outgoing request.
        /// </summary>
        /// <remarks>
        /// If token is unavailable and the request targets a non-anonymous path, this method throws <see cref="UnauthenticatedExpection"/>.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Thrown when <see cref="HttpRequestMessage.RequestUri"/> is null.</exception>
        /// <exception cref="UnauthenticatedExpection">Thrown when no token is available and the path is not anonymous.</exception>
        protected virtual async ValueTask Authenticate(HttpRequestMessage request, CancellationToken cancellationToken)
        {            
            if (request.Method == HttpMethod.Head)
                return;

            if (request.Headers.Authorization != null)
                return;

            if (request.RequestUri == null)
                throw new InvalidOperationException("RequestUri is required");
           
            // request the access token
            var accessToken = await _tokens.GetTokenAsync();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                // set the bearer token to the outgoing request
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                return;
            }

            var path = GetRequestPath(request);
            if (IsAnonymous(request.Method, path))
                return;
        
            throw new UnauthenticatedExpection("access token not available at this time");    
        }

                /// <summary>
                /// Extracts a normalized path from <see cref="HttpRequestMessage.RequestUri"/> to be used by anonymous-path rules.
                /// </summary>
                /// <remarks>
                /// For absolute URIs, uses <see cref="Uri.AbsolutePath"/>.
                /// For relative URIs, uses the string representation and strips the query string.
                /// </remarks>
                /// <exception cref="InvalidOperationException">Thrown when <see cref="HttpRequestMessage.RequestUri"/> is null.</exception>
        protected virtual string GetRequestPath(HttpRequestMessage request)
        {
            if (request.RequestUri == null)
                throw new InvalidOperationException("RequestUri is required");

            if (request.RequestUri.IsAbsoluteUri)
                return request.RequestUri.AbsolutePath;

            return request.RequestUri.ToString().Split('?').First();
        }

        /// <summary>
        /// Determines whether a given path is considered anonymous for a specific HTTP method.
        /// </summary>
        /// <remarks>
        /// Method-specific rules (<see cref="AnonymousPathsByMethod"/>) have precedence over <see cref="AnonymousPaths"/>.
        /// </remarks>
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

        /// <summary>
        /// Determines whether a given path is considered anonymous for all HTTP methods.
        /// </summary>
        protected virtual bool IsAnonymous(string path)
        {
            var normalizedPath = NormalizePath(path);

            if (normalizedPath == "/health") return true;
            if (AnonymousPaths != null && AnonymousPaths.Any(candidate => PathEquals(candidate, normalizedPath))) return true;

            return false;
        }

        /// <summary>
        /// Normalizes a path to a canonical representation used for matching.
        /// </summary>
        /// <remarks>
        /// Ensures the path is non-empty and starts with '/'.
        /// </remarks>
        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "/";

            var normalized = path.Trim();
            if (!normalized.StartsWith("/"))
                normalized = "/" + normalized;

            return normalized;
        }

        /// <summary>
        /// Compares two paths after normalization.
        /// </summary>
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
