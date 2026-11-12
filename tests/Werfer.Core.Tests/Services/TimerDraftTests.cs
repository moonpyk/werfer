using Microsoft.Extensions.Options;
using Werfer.Clockify;
using Werfer.Clockify.Models;

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

        await draft.SaveAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, client.TimeEntryReads);
        Assert.Empty(client.Updates);
    }

    // The draft exists before connecting, as in the app: connecting loads the running entry into it
    private static async Task<(FakeClockifyApiClient Client, TimeTrackingService Service, TimerDraft Draft)>
        ConnectedAsync(TimeEntry running)
    {
        var client = new FakeClockifyApiClient { Running = running, Current = running };
        var service = new TimeTrackingService(client, Options.Create(new ClockifyOptions()));
        var draft = new TimerDraft(service);

        await service.ConnectAsync("api-key", TestContext.Current.CancellationToken);

        return (client, service, draft);
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
