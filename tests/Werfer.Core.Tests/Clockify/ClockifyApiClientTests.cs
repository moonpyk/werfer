using System.Net;
using Refit;

namespace Werfer.Clockify;

public class ClockifyApiClientTests
{
    [Fact]
    public async Task GetTimeEntriesForUser_SendsClockifyParameterNames()
    {
        var (handler, client) = Create();

        await client.GetRawTimeEntriesForUserAsync(
            "w1",
            "u1",
            inProgress: true,
            page: 2,
            pageSize: 1000,
            ctk: TestContext.Current.CancellationToken
        );

        // Refit's [Query("x")] sets a delimiter, not the name: Clockify then ignored page-size and served 50 entries
        var query = Assert.Single(handler.Requests).RequestUri!.Query;
        Assert.Contains("page-size=1000", query);
        Assert.Contains("in-progress=True", query, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetProjects_GeneratedClient_ReadsUntilLastPage()
    {
        // The Refit generated client must keep the interface's paging body, not replace it
        var (handler, client) = Create();

        var projects = await client.GetProjectsAsync("w1", ctk: TestContext.Current.CancellationToken);

        Assert.Equal(["p1", "p2"], projects.Select(p => p.Id));
        Assert.Equal(2, handler.Requests.Count);
    }

    private static (RecordingHandler Handler, IClockifyApiClient Client) Create()
    {
        var handler = new RecordingHandler();
        var client = RestService.ForGenerated<IClockifyApiClient>(
            new HttpClient(handler) { BaseAddress = new Uri("https://api.clockify.me/api/v1") },
            WerferCoreJsonContext.Default
        );

        return (handler, client);
    }

    /// <summary>
    ///     Serves two pages of one item each, the second flagged as the last one
    /// </summary>
    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ctk)
        {
            Requests.Add(request);
            var first = request.RequestUri!.Query.Contains("page=1&") || request.RequestUri.Query.EndsWith("page=1");

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(first ? """[{"id":"p1"}]""" : """[{"id":"p2"}]""")
            };
            response.Headers.Add("Last-Page", first ? "false" : "true");

            return Task.FromResult(response);
        }
    }
}
