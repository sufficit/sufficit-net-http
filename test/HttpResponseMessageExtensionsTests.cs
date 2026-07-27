using System.Net;
using System.Net.Http;
using System.Text;
using Sufficit.Net.Http;

namespace Sufficit.Net.Http.Tests;

public sealed class HttpResponseMessageExtensionsTests
{
    [Fact]
    public async Task EnsureSuccess_UsesTheEndpointMessageInsteadOfRawJson()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = new StringContent(
                """
                {
                  "success": false,
                  "message": "Access denied. Required directive: dialplanupdate.",
                  "data": {
                    "directive": "dialplanupdate"
                  }
                }
                """,
                Encoding.UTF8,
                "application/json"),
        };

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => response.EnsureSuccess().AsTask());

        Assert.Equal(HttpStatusCode.Forbidden, exception.StatusCode);
        Assert.Equal(
            "Access denied. Required directive: dialplanupdate.",
            exception.Message);
    }

    [Fact]
    public async Task EnsureSuccess_PreservesNonJsonErrorBodies()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent("upstream unavailable"),
        };

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => response.EnsureSuccess().AsTask());

        Assert.Equal("upstream unavailable", exception.Message);
    }
}
