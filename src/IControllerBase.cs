using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Sufficit.Net.Http
{
    public interface IControllerBase
    {
        HttpClient Client { get; }

        JsonSerializerOptions Json { get; }

        ILogger Logger { get; }

        Action<bool>? Healthy { get; }
    }
}
