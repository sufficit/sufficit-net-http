using Microsoft.Extensions.Logging;
using Sufficit.Identity;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Sufficit.Net.Http
{
    public sealed class AuthenticatedControllerBase : IAuthenticatedControllerBase
    {
        public ITokenProvider Tokens { get; }

        public HttpClient Client { get; }

        public JsonSerializerOptions Json { get; }

        public ILogger Logger { get; }

        public Action<bool>? Healthy { get; set; }

        public AuthenticatedControllerBase (ITokenProvider tokens, HttpClient client, JsonSerializerOptions json, ILogger logger) 
        { 
            Tokens = tokens;
            Client = client;
            Json = json;
            Logger = logger;
        }
    }
}
