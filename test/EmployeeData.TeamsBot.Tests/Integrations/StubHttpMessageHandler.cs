using System.Net;
using System.Text;

namespace EmployeeData.TeamsBot.Tests.Integrations;

/// <summary>Returns a canned response for any request, so client mapping can be tested without a server.</summary>
internal sealed class StubHttpMessageHandler(HttpStatusCode status, string json) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
}
