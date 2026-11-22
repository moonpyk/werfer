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
        // Another app stopped it: a PUT without end would restart it
        client.Running = null;

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
    public async Task Update_AnotherEntryStartedElsewhere_SendsNothingAndClearsRunning()
    {
        var (client, service) = await ConnectedWithRunningEntryAsync();
        // Another app stopped it and started a new one: the update must not land on either
        var other = RunningEntry();
        other.Id = "e2";
        client.Running = other;

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
    public async Task DeleteRunning_DeletesItAndClearsTheRunningEntry()
    {
        var (client, service) = await ConnectedWithRunningEntryAsync();

        await service.DeleteRunningEntryAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["e1"], client.Deletes);
        Assert.Null(service.RunningEntry);
    }

    [Fact]
    public async Task UpdateStart_MovesTheStartAndKeepsTheOtherFields()
    {
        var (client, service) = await ConnectedWithRunningEntryAsync();
        var earlier = Started.AddMinutes(-15);

        await service.UpdateRunningEntryStartAsync(earlier, TestContext.Current.CancellationToken);

        var (id, request) = Assert.Single(client.Updates);
        Assert.Equal("e1", id);
        Assert.Equal(earlier, request.Start);
        Assert.Null(request.End);
        Assert.Equal("p1", request.ProjectId);
        Assert.Equal("t1", request.TaskId);
        Assert.Equal("JIRA-1", request.Description);
        Assert.True(request.Billable);
        Assert.Equal(["tag"], request.TagIds);
        Assert.Equal(earlier, service.RunningEntry?.TimeInterval?.Start);
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

    [Fact]
    public async Task UpdateFinished_ResendsTheFieldsClockifyWouldClear()
    {
        var (client, service) = await ConnectedWithRunningEntryAsync();
        var end = Started.AddHours(1);

        await service.UpdateTimeEntryAsync(
            RunningEntry(end),
            new TimeRange(Started, end),
            "p2",
            "t2",
            "  JIRA-2  ",
            TestContext.Current.CancellationToken
        );

        var (id, request) = Assert.Single(client.Updates);

        Assert.Equal("e1", id);
        Assert.Equal(Started, request.Start);
        Assert.Equal(end, request.End);
        Assert.True(request.Billable);
        Assert.Equal(["tag"], request.TagIds);
        Assert.Equal("p2", request.ProjectId);
        Assert.Equal("t2", request.TaskId);
        Assert.Equal("JIRA-2", request.Description);
    }

    [Fact]
    public async Task Split_ShortensTheEntryAndCreatesTheSecondHalf()
    {
        var (client, service) = await ConnectedWithRunningEntryAsync();
        var end = Started.AddHours(1);
        var at = Started.AddMinutes(20);

        await service.SplitTimeEntryAsync(RunningEntry(end), at, TestContext.Current.CancellationToken);

        // Both halves keep every field: Clockify's create and update would leave the missing ones empty
        var second = Assert.Single(client.Creates);
        Assert.Equal(at, second.Start);
        Assert.Equal(end, second.End);
        Assert.Equal("p1", second.ProjectId);
        Assert.Equal("t1", second.TaskId);
        Assert.Equal("JIRA-1", second.Description);
        Assert.True(second.Billable);
        Assert.Equal(["tag"], second.TagIds);

        var (id, first) = Assert.Single(client.Updates);
        Assert.Equal("e1", id);
        Assert.Equal(Started, first.Start);
        Assert.Equal(at, first.End);
        Assert.Equal("JIRA-1", first.Description);
        Assert.Equal(["tag"], first.TagIds);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(60)]
    public async Task Split_OnTheEntryEdge_IsRefused(int minutes)
    {
        var (client, service) = await ConnectedWithRunningEntryAsync();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.SplitTimeEntryAsync(
            RunningEntry(Started.AddHours(1)),
            Started.AddMinutes(minutes),
            TestContext.Current.CancellationToken
        ));

        Assert.Empty(client.Creates);
        Assert.Empty(client.Updates);
    }

    [Theory]
    [InlineData("JIRA-1")]
    // Same once trimmed
    [InlineData("  JIRA-1 ")]
    public async Task Update_Unchanged_SendsNothing(string description)
    {
        var (client, service) = await ConnectedWithRunningEntryAsync();
        var reads = client.RunningReads;

        await service.UpdateRunningEntryAsync(
            "p1",
            "t1",
            description,
            TestContext.Current.CancellationToken
        );

        Assert.Empty(client.Updates);
        Assert.Equal(reads, client.RunningReads);
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

        client.StopException = await ApiErrorAsync(HttpStatusCode.NotFound);

        await service.StopAsync(
            TestContext.Current.CancellationToken
        );

        Assert.Null(service.RunningEntry);
    }

    [Fact]
    public async Task Connect_RejectedKey_KeepsTheWorkingOne()
    {
        var client = new FakeClockifyApiClient();
        var options = Options.Create(new ClockifyOptions());
        var service = new TimeTrackingService(client, options);

        await service.ConnectAsync(
            "good-key",
            TestContext.Current.CancellationToken
        );

        // A typo in Settings: the running timer must still be stoppable with the key that works
        client.UserException = await ApiErrorAsync(HttpStatusCode.Unauthorized);

        await Assert.ThrowsAsync<ApiException>(() => service.ConnectAsync(
            "bad-key",
            TestContext.Current.CancellationToken
        ));

        Assert.Equal("good-key", options.Value.ApiKey);
    }

    [Fact]
    public async Task Connect_PublishesTheProjectsActiveTasks()
    {
        var client = new FakeClockifyApiClient();
        client.Projects.Add(new Project
        {
            Id = "p1",
            Tasks =
            [
                new ProjectTask { Id = "t1", Status = "ACTIVE" },
                new ProjectTask { Id = "t2", Status = "DONE" }
            ]
        });
        var service = new TimeTrackingService(client, Options.Create(new ClockifyOptions()));

        await service.ConnectAsync("api-key", TestContext.Current.CancellationToken);

        Assert.NotNull(service.Session);
        Assert.Equal("w1", service.Session.WorkspaceId);
        Assert.Equal("t1", Assert.Single(service.Session.Choices).Task.Id);
    }

    [Fact]
    public async Task Connect_ProjectsFail_KeepsThePreviousSession()
    {
        var (client, service) = await ConnectedWithRunningEntryAsync();
        var session = service.Session;
        var running = service.RunningEntry;
        var connected = 0;
        service.Connected += (_, _) => connected++;

        // The user is found, then the network drops: no half-connected state with a user but no projects
        client.ProjectsException = new HttpRequestException("Network down");

        await Assert.ThrowsAsync<HttpRequestException>(() => service.ConnectAsync(
            "other-key",
            TestContext.Current.CancellationToken
        ));

        Assert.Same(session, service.Session);
        Assert.Same(running, service.RunningEntry);
        Assert.Equal(0, connected);
    }

    [Fact]
    public async Task GetTimeEntriesSince_PageSizeCapped_ReadsEveryPage()
    {
        var (client, service) = await ConnectedWithRunningEntryAsync();
        // Clockify serves fewer than asked without error: the oldest entries, on the last pages, mustn't be lost
        client.MaxPageSize = 2;
        client.Entries.AddRange(Enumerable.Range(1, 5).Select(i => new TimeEntry { Id = $"h{i}" }));

        var entries = await service.GetTimeEntriesSinceAsync(Started, TestContext.Current.CancellationToken);

        Assert.Equal([
            "h1",
            "h2",
            "h3",
            "h4",
            "h5"
        ], entries.Select(e => e.Id));
    }

    private static Task<ApiException> ApiErrorAsync(HttpStatusCode status)
    {
        return ApiException.Create(
            new HttpRequestMessage(HttpMethod.Get, "https://api.clockify.me/"),
            HttpMethod.Get,
            new HttpResponseMessage(status),
            new RefitSettings()
        );
    }

    private static async Task<(FakeClockifyApiClient Client, TimeTrackingService Service)>
        ConnectedWithRunningEntryAsync()
    {
        var client = new FakeClockifyApiClient { Running = RunningEntry() };

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
