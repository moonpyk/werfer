using Microsoft.Extensions.Options;
using Werfer.Clockify;
using Werfer.Clockify.Models;
using Werfer.Clockify.ViewModels;

namespace Werfer.Services;

public class TimerDraftTests
{
    [Fact]
    public async Task ReplyForTheSameEntry_KeepsWhatIsTyped()
    {
        var (_, service, draft) = await ConnectedAsync(RunningEntry());
        Assert.Equal("JIRA-1", draft.Description);

        // Typed while an update of the same entry (a project pick) was on its way
        draft.Description = "JIRA-2 being typed";
        await service.UpdateRunningEntryAsync(
            "p2",
            "t2",
            "JIRA-1",
            TestContext.Current.CancellationToken
        );

        Assert.Equal("JIRA-2 being typed", draft.Description);
    }

    [Fact]
    public async Task Refresh_TakesTheChangesMadeElsewhere()
    {
        var (client, _, draft) = await ConnectedAsync(RunningEntry());
        var edited = RunningEntry();
        edited.Description = "JIRA-9";
        client.Running = edited;

        await draft.RefreshAsync(TestContext.Current.CancellationToken);

        // Nothing typed: the save before the refresh sent nothing
        Assert.Empty(client.Updates);
        Assert.Equal("JIRA-9", draft.Description);
    }

    [Fact]
    public async Task Save_EntryWithoutTask_SendsTheDescription()
    {
        // Started elsewhere on a project only: not among the choices, which all have a task
        var entry = RunningEntry();
        entry.TaskId = null;
        var (client, _, draft) = await ConnectedAsync(entry);

        draft.Description = "JIRA-2";
        await draft.SaveAsync(TestContext.Current.CancellationToken);

        var (_, request) = Assert.Single(client.Updates);
        Assert.Equal("p1", request.ProjectId);
        Assert.Null(request.TaskId);
        Assert.Equal("JIRA-2", request.Description);
    }

    [Fact]
    public async Task Save_NoDescription_SendsNothing()
    {
        // Clockify sends "" for no description, the draft's null is the same
        var entry = RunningEntry();
        entry.Description = "";
        var (client, _, draft) = await ConnectedAsync(entry);
        var reads = client.RunningReads;

        await draft.SaveAsync(TestContext.Current.CancellationToken);

        Assert.Equal(reads, client.RunningReads);
        Assert.Empty(client.Updates);
    }

    [Fact]
    public async Task Refresh_SaveFails_KeepsWhatIsTyped()
    {
        var (client, _, draft) = await ConnectedAsync(RunningEntry());
        client.UpdateException = new HttpRequestException("Network down");

        draft.Description = "JIRA-2 typed";
        await Assert.ThrowsAsync<HttpRequestException>(() => draft.RefreshAsync(TestContext.Current.CancellationToken));

        // Reloading the entry as Clockify has it would put back JIRA-1: what was typed would be lost
        Assert.Equal("JIRA-2 typed", draft.Description);
    }

    [Fact]
    public async Task Stop_SaveFails_StillStops()
    {
        var (client, service, draft) = await ConnectedAsync(RunningEntry());
        client.UpdateException = new HttpRequestException("Network down");

        draft.Description = "JIRA-2 typed";
        await Assert.ThrowsAsync<HttpRequestException>(() => draft.StopAsync(TestContext.Current.CancellationToken));

        // The user asked to stop: the timer mustn't keep running because the description couldn't be sent
        Assert.Null(service.RunningEntry);
        Assert.Equal("JIRA-2 typed", draft.Description);
    }

    [Fact]
    public async Task Reset_StopFails_KeepsTheFields()
    {
        var (client, service, draft) = await ConnectedAsync(RunningEntry());
        client.StopException = new HttpRequestException("Network down");
        var choice = draft.Choice;

        draft.Description = "JIRA-2 typed";
        await Assert.ThrowsAsync<HttpRequestException>(() => draft.ResetAsync(TestContext.Current.CancellationToken));

        // Still running: emptied fields would be sent to it on the next save
        Assert.NotNull(service.RunningEntry);
        Assert.Equal("JIRA-2 typed", draft.Description);
        Assert.Equal(choice, draft.Choice);
    }

    // The draft exists before connecting, as in the app: connecting loads the running entry into it
    private static async Task<(FakeClockifyApiClient Client, TimeTrackingService Service, TimerDraft Draft)>
        ConnectedAsync(TimeEntry? running, FakeApplicationSettings? settings = null)
    {
        var client = new FakeClockifyApiClient { Running = running };
        var service = new TimeTrackingService(client, Options.Create(new ClockifyOptions()));
        var draft = new TimerDraft(service, settings ?? new FakeApplicationSettings());

        await service.ConnectAsync("api-key", TestContext.Current.CancellationToken);

        return (client, service, draft);
    }

    [Fact]
    public async Task Start_NoChoice_DefaultOff_StartsNothing()
    {
        var (client, _, draft) = await StoppedWithChoicesAsync(new FakeApplicationSettings());

        Assert.False(await draft.StartAsync(TestContext.Current.CancellationToken));
        Assert.Empty(client.Creates);
    }

    [Fact]
    public async Task Start_NoChoice_StartsOnTheDefaultProjectAndTask()
    {
        var (client, _, draft) = await StoppedWithChoicesAsync(new FakeApplicationSettings
        {
            DefaultChoiceEnabled = true,
            DefaultProjectId = "p2",
            DefaultTaskId = "t2"
        });

        Assert.True(await draft.StartAsync(TestContext.Current.CancellationToken));

        var request = Assert.Single(client.Creates);
        Assert.Equal(("p2", "t2"), (request.ProjectId, request.TaskId));
        // The bar shows what the timer runs on
        Assert.Equal("t2", draft.Choice?.Task.Id);
    }

    [Fact]
    public async Task Start_NoChoice_LastUsed_StartsOnTheLastEntrysProjectAndTask()
    {
        var (client, _, draft) = await StoppedWithChoicesAsync(new FakeApplicationSettings
        {
            DefaultChoiceEnabled = true
        });
        // Most recent first, as Clockify serves them
        client.Entries.Add(Entry("old2", "p2", "t2"));
        client.Entries.Add(Entry("old1", "p1", "t1"));

        Assert.True(await draft.StartAsync(TestContext.Current.CancellationToken));

        var request = Assert.Single(client.Creates);
        Assert.Equal(("p2", "t2"), (request.ProjectId, request.TaskId));
    }

    [Fact]
    public async Task Start_DefaultNotStartableAnymore_StartsNothing()
    {
        // An archived project or a done task is no longer among the choices
        var (client, _, draft) = await StoppedWithChoicesAsync(new FakeApplicationSettings
        {
            DefaultChoiceEnabled = true,
            DefaultProjectId = "archived",
            DefaultTaskId = "t9"
        });
        var changes = 0;
        draft.Changed += (_, _) => changes++;

        Assert.False(await draft.StartAsync(TestContext.Current.CancellationToken));
        Assert.Empty(client.Creates);
        // Nothing changed: the UI must not clear a search typed in the project box
        Assert.Equal(0, changes);
    }

    [Fact]
    public async Task Start_WithAChoice_IgnoresTheDefault()
    {
        var (client, _, draft) = await StoppedWithChoicesAsync(new FakeApplicationSettings
        {
            DefaultChoiceEnabled = true,
            DefaultProjectId = "p2",
            DefaultTaskId = "t2"
        });
        draft.Choice = draft.Choices[0];

        Assert.True(await draft.StartAsync(TestContext.Current.CancellationToken));

        Assert.Equal("t1", Assert.Single(client.Creates).TaskId);
    }

    // Nothing running, two startable pairs: p1/t1 and p2/t2
    private static async Task<(FakeClockifyApiClient Client, TimeTrackingService Service, TimerDraft Draft)>
        StoppedWithChoicesAsync(FakeApplicationSettings settings)
    {
        var connected = await ConnectedAsync(null, settings);
        connected.Draft.Choices = [Choice("p1", "t1"), Choice("p2", "t2")];

        return connected;
    }

    private static ProjectTaskChoice Choice(string projectId, string taskId)
    {
        return new ProjectTaskChoice(new Project { Id = projectId }, new ProjectTask { Id = taskId });
    }

    private static TimeEntry Entry(string id, string projectId, string taskId)
    {
        return new TimeEntry
        {
            Id = id,
            ProjectId = projectId,
            TaskId = taskId,
            TimeInterval = new TimeInterval
            {
                Start = DateTimeOffset.UtcNow.AddHours(-2),
                End = DateTimeOffset.UtcNow.AddHours(-1)
            }
        };
    }

    private static TimeEntry RunningEntry()
    {
        return new TimeEntry
        {
            Id = "e1",
            ProjectId = "p1",
            TaskId = "t1",
            Description = "JIRA-1",
            TimeInterval = new TimeInterval { Start = DateTimeOffset.UtcNow.AddHours(-1) }
        };
    }
}
