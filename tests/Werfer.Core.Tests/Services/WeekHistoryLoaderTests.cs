using Microsoft.Extensions.Options;
using Werfer.Clockify;
using Werfer.Clockify.Models;

namespace Werfer.Services;

public class WeekHistoryLoaderTests
{
    private static readonly DateTimeOffset Now = new(
        2026, 
        4, 
        1, 
        8, 
        0, 
        0, 
        TimeSpan.Zero
        );

    [Fact]
    public async Task Load_AnsweredOutOfOrder_OnlyTheLatestGetsItsResult()
    {
        var (client, _, loader) = await ConnectedAsync();
        var firstGate = new TaskCompletionSource();
        var secondGate = new TaskCompletionSource();
        client.EntriesGates.Enqueue(firstGate);
        client.EntriesGates.Enqueue(secondGate);
        client.Entries.Add(new TimeEntry { Id = "h1" });

        // The first load started before a save, the second after: the first one answers last
        var first = loader.LoadAsync(Now, TimeZoneInfo.Utc, TestContext.Current.CancellationToken);
        var second = loader.LoadAsync(Now, TimeZoneInfo.Utc, TestContext.Current.CancellationToken);
        secondGate.SetResult();
        firstGate.SetResult();

        Assert.Equal(["h1"], (await second)!.Select(e => e.Id));
        Assert.Null(await first);
    }

    [Fact]
    public async Task Load_AnotherAccountConnected_IsAnotherAccountUntilLoaded()
    {
        var (client, service, loader) = await ConnectedAsync();
        await loader.LoadAsync(Now, TimeZoneInfo.Utc, TestContext.Current.CancellationToken);
        Assert.False(loader.IsAnotherAccount);

        client.User = new User { Id = "u2", ActiveWorkspace = "w1" };
        await service.ConnectAsync("other-key", TestContext.Current.CancellationToken);
        Assert.True(loader.IsAnotherAccount);

        await loader.LoadAsync(Now, TimeZoneInfo.Utc, TestContext.Current.CancellationToken);
        Assert.False(loader.IsAnotherAccount);
    }

    private static async Task<(FakeClockifyApiClient Client, TimeTrackingService Service, WeekHistoryLoader Loader)>
        ConnectedAsync()
    {
        var client = new FakeClockifyApiClient();
        var service = new TimeTrackingService(client, Options.Create(new ClockifyOptions()));
        await service.ConnectAsync("api-key", TestContext.Current.CancellationToken);

        return (client, service, new WeekHistoryLoader(service, new FakeApplicationSettings()));
    }
}
