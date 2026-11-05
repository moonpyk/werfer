using System.Net;
using Microsoft.Extensions.Options;
using Refit;
using Werfer.Clockify;
using Werfer.Clockify.Models;

namespace Werfer.Services;

public class TimeTrackingServiceTests
{
    private static readonly DateTimeOffset Started = new(
        2026,
        4,
        1,
        8,
        0,
        0,
        TimeSpan.Zero
    );

    [Fact]
    public async Task Update_EntryStoppedElsewhere_SendsNothingAndClearsRunning()
    {
        var (client, service) = await ConnectedWithRunningEntryAsync();
        // Another client stopped it: a PUT without end would restart it
        client.Current = RunningEntry(Started.AddHours(1));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateRunningEntryAsync(
            "p2",
            "t2",
            "JIRA-2",
            TestContext.Current.CancellationToken
        ));

        Assert.Empty(client.Updates);
        Assert.Null(service.RunningEntry);
    }

    [Fact]
    public async Task Update_ResendsTheFieldsClockifyWouldClear()
    {
        var (client, service) = await ConnectedWithRunningEntryAsync();

        await service.UpdateRunningEntryAsync(
            "p2",
            "t2",
            "  JIRA-2  ",
            TestContext.Current.CancellationToken
        );

        // Clockify's update is a full replace: a missing start, tag or billable flag would be lost
        var (id, request) = Assert.Single(client.Updates);
        Assert.Equal("e1", id);
        Assert.Equal(Started, request.Start);
        Assert.Null(request.End);
        Assert.True(request.Billable);
        Assert.Equal(["tag"], request.TagIds);
        Assert.Equal("p2", request.ProjectId);
        Assert.Equal("t2", request.TaskId);
        Assert.Equal("JIRA-2", request.Description);
        Assert.Equal("JIRA-2", service.RunningEntry?.Description);
    }

    [Theory]
    [InlineData("JIRA-1")]
    // Same once trimmed
    [InlineData("  JIRA-1 ")]
    public async Task Update_Unchanged_SendsNothing(string description)
    {
        var (client, service) = await ConnectedWithRunningEntryAsync();

        await service.UpdateRunningEntryAsync(
            "p1",
            "t1",
            description,
            TestContext.Current.CancellationToken
        );

        Assert.Empty(client.Updates);
        Assert.Equal(0, client.TimeEntryReads);
    }

    [Fact]
    public async Task Update_QueuedBehindStop_DoesNotRestartTheEntry()
    {
        var (client, service) = await ConnectedWithRunningEntryAsync();
        client.StopGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // The description is committed on lost focus, often right as the user clicks stop
        var stop = service.StopAsync(
            TestContext.Current.CancellationToken
        );
        var update = service.UpdateRunningEntryAsync(
            "p1",
            "t1",
            "JIRA-2",
            TestContext.Current.CancellationToken
        );
        client.StopGate.SetResult();

        await stop;
        await update;

        Assert.Empty(client.Updates);
        Assert.Null(service.RunningEntry);
    }

    [Fact]
    public async Task Stop_AlreadyStoppedElsewhere_Succeeds()
    {
        var (client, service) = await ConnectedWithRunningEntryAsync();

        client.StopException = await ApiException.Create(
            new HttpRequestMessage(HttpMethod.Patch, "https://api.clockify.me/"),
            HttpMethod.Patch,
            new HttpResponseMessage(HttpStatusCode.NotFound),
            new RefitSettings()
        );

        await service.StopAsync(
            TestContext.Current.CancellationToken
        );

        Assert.Null(service.RunningEntry);
    }

    private static async Task<(FakeClockifyApiClient Client, TimeTrackingService Service)>
        ConnectedWithRunningEntryAsync()
    {
        var client = new FakeClockifyApiClient { Running = RunningEntry() };
        client.Current = client.Running;

        var service = new TimeTrackingService(client, Options.Create(new ClockifyOptions()));
        await service.ConnectAsync("api-key");

        Assert.NotNull(service.RunningEntry);
        return (client, service);
    }

    private static TimeEntry RunningEntry(DateTimeOffset? end = null)
    {
        return new TimeEntry
        {
            Id = "e1",
            ProjectId = "p1",
            TaskId = "t1",
            Description = "JIRA-1",
            Billable = true,
            TagIds = ["tag"],
            TimeInterval = new TimeInterval { Start = Started, End = end }
        };
    }
}
