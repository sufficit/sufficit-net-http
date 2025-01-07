using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sufficit.Net.Http
{
    public class PreConfiguredHttpClient : HttpClient
    {
        public PreConfiguredHttpClient (IHttpClientOptions options)
        {
            Configure(options);
        }

        protected virtual void Configure (IHttpClientOptions options)
        {
            BaseAddress = new Uri(options.BaseAddress);

            if (options.TimeOut.HasValue)
                Timeout = TimeSpan.FromSeconds(options.TimeOut.Value);

            if (!string.IsNullOrWhiteSpace(options.UserAgent))
            {
                var productValue = new ProductInfoHeaderValue($"({options.UserAgent})");
                DefaultRequestHeaders.UserAgent.Add(productValue);
            }
        }
    }
}
