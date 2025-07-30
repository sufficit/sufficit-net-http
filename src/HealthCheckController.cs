using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sufficit.Net.Http
{
    public class HealthCheckController
    {
        readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        private readonly HttpClient _client;

        /// <summary>
        ///     Healthy or UnHealthy status
        /// </summary>
        public bool Available { get; private set; }

        /// <summary>
        ///     Last timestamp for health checked
        /// </summary>
        public DateTime Checked { get; internal set; }

        /// <summary>
        ///     Status changed
        /// </summary>
        public event EventHandler<bool>? OnChanged;

        public HealthCheckController (HttpClient client) => _client = client;

        /// <summary>
        ///     Used on component initialization for ensure ready status
        /// </summary>
        public async Task GetStatus()
        {
            await _semaphore.WaitAsync();
            if (Checked == DateTime.MinValue || DateTime.UtcNow.Subtract(Checked).TotalMinutes > 30)
                _ = await Health(default);

            _semaphore.Release();
        }

        /// <summary>
        ///     Sets a value for health status, used internal. <br />
        ///     Or you can set a custom value for testing purposes
        /// </summary>
        public void Healthy (bool value = true)
        {
            // updating timestamp
            Checked = DateTime.UtcNow;

            if (Available != value)
            {
                Available = value;
                OnChanged?.Invoke(this, value);
            }
        }

        public async Task<HealthResponse> Health (CancellationToken cancellationToken)
        {
            bool status = false;
            HealthResponse? response;
            try
            {
                response = await _client.GetFromJsonAsync<HealthResponse>("/health", cancellationToken);
                if (response != null)
                    status = response.Status == "Healthy";
            }
            catch (Exception ex)
            {
                response = new HealthResponse() { Status = $"UnHealthy: {ex.Message}" };
            }

            Healthy(status);
            return response ?? new HealthResponse() { Status = "UnHealthy: null response" };
        }
    }
}
