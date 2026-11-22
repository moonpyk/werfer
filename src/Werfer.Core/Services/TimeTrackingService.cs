using System.Net;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using Refit;
using Werfer.Clockify;
using Werfer.Clockify.Models;
using Werfer.Clockify.Requests;

namespace Werfer.Services;

/// <summary>
///     Clockify session shared by every screen: current user, workspace and running time entry.
/// </summary>
[PublicAPI]
public class TimeTrackingService(
    IClockifyApiClient client,
    IOptions<ClockifyOptions> options
)
{
    /// <summary>
    ///     Runs one operation at a time. Overlapping calls would act on an outdated RunningEntry: for example, a
    ///     description update finishing after a stop would restart the entry. Waiting is async, so queued calls don't block
    ///     the UI
    /// </summary>
    private SemaphoreSlim Operations { get; } = new(1, 1);

    private IClockifyApiClient Client { get; } = client;
    private IOptions<ClockifyOptions> Options { get; } = options;

    /// <summary>
    ///     What the last successful connection loaded, null until then. Replaced whole by each connection, see
    ///     <see cref="Session" />.
    /// </summary>
    public Session? Session { get; private set; }

    /// <summary>
    ///     The connected user, null until connected.
    /// </summary>
    public User? User => Session?.User;

    /// <summary>
    ///     The entry currently being timed, null when the timer is stopped.
    /// </summary>
    public TimeEntry? RunningEntry
    {
        get;
        private set
        {
            field = value;
            RunningEntryChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private Session CurrentSession => Session ?? throw new InvalidOperationException("Not connected to Clockify");

    /// <summary>
    ///     Raised whenever <see cref="RunningEntry" /> is replaced, started, updated or stopped.
    /// </summary>
    public event EventHandler? RunningEntryChanged;

    /// <summary>
    ///     Raised after a successful <see cref="ConnectAsync" />, once <see cref="Session" /> and the running entry are
    ///     published.
    /// </summary>
    public event EventHandler? Connected;

    /// <summary>
    ///     Applies the API key, loads the user, the projects and the entry already running on Clockify, then publishes
    ///     them. If any of it fails, nothing is published: the previous session, its key and its running entry stay.
    /// </summary>
    public Task<User> ConnectAsync(string apiKey, CancellationToken ctk = default)
    {
        return ExclusiveAsync(async () =>
        {
            // The auth handler reads this same options instance, so the next call uses the new key
            var previousKey = Options.Value.ApiKey;
            Options.Value.ApiKey = apiKey.Trim();

            Session session;
            TimeEntry? running;

            try
            {
                session = await Session.LoadAsync(Client, ctk);
                running = await GetRunningEntryAsync(session, ctk);
            }
            catch
            {
                // A failed connection must not replace the key the current session (and its running timer) uses
                Options.Value.ApiKey = previousKey;
                throw;
            }

            // Session first: whoever handles RunningEntryChanged finds the new session's projects
            Session = session;
            RunningEntry = running;

            Connected?.Invoke(this, EventArgs.Empty);

            return session.User;
        }, ctk);
    }

    /// <summary>
    ///     Reloads the running entry from Clockify: it may have been started or stopped in another app.
    /// </summary>
    public Task RefreshRunningEntryAsync(CancellationToken ctk = default)
    {
        return ExclusiveAsync(async () =>
        {
            RunningEntry = await GetRunningEntryAsync(CurrentSession, ctk);
        }, ctk);
    }

    private async Task<TimeEntry?> GetRunningEntryAsync(Session session, CancellationToken ctk)
    {
        // Asks for the in-progress entry only: the latest entry by start time may be a finished one added by hand
        var last = (
            await Client.GetTimeEntriesForUserAsync(
                session.WorkspaceId,
                session.User.Id,
                inProgress: true,
                ctk: ctk
            )
        ).FirstOrDefault();

        return last?.TimeInterval is { End: null } ? last : null;
    }

    /// <summary>
    ///     The user's entries started since <paramref name="start" />, including the running one.
    /// </summary>
    public Task<List<TimeEntry>> GetTimeEntriesSinceAsync(DateTimeOffset start, CancellationToken ctk = default)
    {
        // Read only, so no lock: it would wait behind a start or stop for no reason
        return Client.GetTimeEntriesForUserAsync(
            CurrentSession.WorkspaceId,
            CurrentSession.User.Id,
            start,
            ctk: ctk
        );
    }

    /// <summary>
    ///     The user's most recent entry, running or not; null when there is none.
    /// </summary>
    public async Task<TimeEntry?> GetLastTimeEntryAsync(CancellationToken ctk = default)
    {
        // One page of one entry: Clockify serves the most recent first
        using var response = await Client.GetRawTimeEntriesForUserAsync(
            CurrentSession.WorkspaceId,
            CurrentSession.User.Id,
            page: 1,
            pageSize: 1,
            ctk: ctk
        );
        await response.EnsureSuccessfulAsync();

        return response.Content?.FirstOrDefault();
    }

    /// <summary>
    ///     Deletes finished entries, one by one: a failure leaves the earlier ones deleted.
    /// </summary>
    public async Task DeleteTimeEntriesAsync(IEnumerable<string> ids, CancellationToken ctk = default)
    {
        foreach (var id in ids)
        {
            await Client.DeleteTimeEntryAsync(CurrentSession.WorkspaceId, id, ctk);
        }
    }

    /// <summary>
    ///     Deletes the running entry, does nothing when stopped.
    /// </summary>
    public Task DeleteRunningEntryAsync(CancellationToken ctk = default)
    {
        return ExclusiveAsync(async () =>
        {
            if (RunningEntry is not { } running)
            {
                return;
            }

            try
            {
                await Client.DeleteTimeEntryAsync(CurrentSession.WorkspaceId, running.Id, ctk);
            }
            catch (ApiException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                // Already deleted in another app: that's what we wanted
            }

            RunningEntry = null;
        }, ctk);
    }

    /// <summary>
    ///     Starts tracking time for the specified project and task in Clockify.
    /// </summary>
    public Task StartAsync(
        string projectId,
        string taskId,
        string? description,
        CancellationToken ctk = default
    )
    {
        return ExclusiveAsync(async () =>
        {
            RunningEntry = await Client.CreateTimeEntryAsync(CurrentSession.WorkspaceId, new CreateTimeEntryRequest
            {
                Start = DateTimeOffset.UtcNow,
                ProjectId = projectId,
                TaskId = taskId,
                Description = Normalize(description)
            }, ctk);
        }, ctk);
    }

    public Task StopAsync(CancellationToken ctk = default)
    {
        return ExclusiveAsync(async () =>
        {
            try
            {
                await Client.StopActiveTimeEntryAsync(
                    CurrentSession.WorkspaceId,
                    CurrentSession.User.Id,
                    new StopTimeEntryRequest { End = DateTimeOffset.UtcNow },
                    ctk
                );
            }
            catch (ApiException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                // Nothing runs anymore, it was already stopped in another app: that's what we wanted
            }

            RunningEntry = null;
        }, ctk);
    }

    /// <summary>
    ///     Changes the project, task and description of the running entry, does nothing when stopped or unchanged.
    /// </summary>
    public Task UpdateRunningEntryAsync(
        string? projectId,
        string? taskId,
        string? description,
        CancellationToken ctk = default
    )
    {
        // Compared once the lock is held, against the entry as the previous operation left it
        return ExclusiveAsync(
            () => UpdateRunningEntryCoreAsync(
                projectId,
                taskId,
                Normalize(description),
                null,
                ctk
            ),
            ctk
        );
    }

    /// <summary>
    ///     Moves the running entry's start, its other fields are kept. Does nothing when stopped or unchanged.
    /// </summary>
    public Task UpdateRunningEntryStartAsync(DateTimeOffset start, CancellationToken ctk = default)
    {
        return ExclusiveAsync(
            () => RunningEntry is { } running
                ? UpdateRunningEntryCoreAsync(
                    running.ProjectId,
                    running.TaskId,
                    Normalize(running.Description),
                    start,
                    ctk
                )
                : Task.CompletedTask,
            ctk
        );
    }

    // A null start keeps the running entry's start
    private async Task UpdateRunningEntryCoreAsync(
        string? projectId,
        string? taskId,
        string? description,
        DateTimeOffset? start,
        CancellationToken ctk
    )
    {
        var running = RunningEntry;
        if (running is null
            // Normalized too: Clockify sends "" for no description, which mustn't count as a change
            || (running.ProjectId == projectId && running.TaskId == taskId &&
                Normalize(running.Description) == description &&
                (start is null || start == running.TimeInterval?.Start)))
        {
            return;
        }

        // Read it again: it may have been stopped or edited in another app, and a PUT without an end would restart it.
        // Through the in-progress query, not GET /time-entries/{id}: Clockify answered that one with 403 Forbidden
        var current = await GetRunningEntryAsync(CurrentSession, ctk);

        // Nothing runs anymore, or another entry was started since
        if (current?.Id != running.Id || current.TimeInterval is not { End: null })
        {
            RunningEntry = null;
            throw new InvalidOperationException("The timer was stopped from another client, changes not saved");
        }

        // Clockify's update is a full replace, so every field we know about is resent
        // shortcut: custom field values aren't read nor resent, so an update clears them; read and resend Clockify's
        // customFieldValues if the workspace starts using them
        RunningEntry = await Client.UpdateTimeEntryAsync(
            CurrentSession.WorkspaceId,
            current.Id,
            new CreateTimeEntryRequest
            {
                Start = start ?? current.TimeInterval.Start,
                ProjectId = projectId,
                TaskId = taskId,
                Billable = current.Billable,
                TagIds = current.TagIds,
                Description = description
            },
            ctk
        );
    }

    /// <summary>
    ///     Changes a finished entry's times, project, task and description; its other fields are kept.
    /// </summary>
    public Task UpdateTimeEntryAsync(
        TimeEntry entry,
        TimeRange range,
        string? projectId,
        string? taskId,
        string? description,
        CancellationToken ctk = default
    )
    {
        // Clockify's update is a full replace, see UpdateRunningEntryCoreAsync
        return ExclusiveAsync(() => Client.UpdateTimeEntryAsync(
            CurrentSession.WorkspaceId,
            entry.Id,
            new CreateTimeEntryRequest
            {
                Start = range.Start,
                End = range.End,
                ProjectId = projectId,
                TaskId = taskId,
                Billable = entry.Billable,
                TagIds = entry.TagIds,
                Description = Normalize(description)
            },
            ctk
        ), ctk);
    }

    /// <summary>
    ///     Cuts a finished entry in two at <paramref name="at" />, strictly inside it: the entry ends there, and a copy
    ///     of it, same project, task, description, billable flag and tags, runs from there to the original end.
    /// </summary>
    public Task SplitTimeEntryAsync(TimeEntry entry, DateTimeOffset at, CancellationToken ctk = default)
    {
        var interval = entry.TimeInterval;
        if (interval?.End is not { } end || at <= interval.Start || at >= end)
        {
            throw new ArgumentOutOfRangeException(
                nameof(at), 
                at,
                "The split must fall strictly inside a finished entry"
            );
        }

        // Clockify can't split: two requests. The update comes first because sending it again changes nothing: after a
        // failure, the sheet stays open and Save again finishes the job. Created first, the second half would be
        // created twice
        return ExclusiveAsync(async () =>
        {
            var workspaceId = CurrentSession.WorkspaceId;
            await Client.UpdateTimeEntryAsync(workspaceId, entry.Id, CopyOf(entry, interval.Start, at), ctk);
            await Client.CreateTimeEntryAsync(workspaceId, CopyOf(entry, at, end), ctk);
        }, ctk);
    }

    // The entry's fields between other times. Clockify's update is a full replace, see UpdateRunningEntryCoreAsync
    private static CreateTimeEntryRequest CopyOf(TimeEntry entry, DateTimeOffset start, DateTimeOffset end)
    {
        return new CreateTimeEntryRequest
        {
            Start = start,
            End = end,
            ProjectId = entry.ProjectId,
            TaskId = entry.TaskId,
            Billable = entry.Billable,
            TagIds = entry.TagIds,
            Description = Normalize(entry.Description)
        };
    }

    private Task ExclusiveAsync(Func<Task> action, CancellationToken ctk)
    {
        return ExclusiveAsync(async () =>
        {
            await action();
            return true;
        }, ctk);
    }

    private async Task<T> ExclusiveAsync<T>(Func<Task<T>> action, CancellationToken ctk)
    {
        // No [MethodImpl(MethodImplOptions.AggressiveInlining)] on purpose: every caller then makes an HTTP request
        // (tens to hundreds of ms), next to which a saved call (~1 ns) is noise. The attribute would only remove that
        // call anyway, not the real overhead (closure, async state machine, delegate invocation), and ILC already
        // inlines a method this small on its own when worthwhile.
        await Operations.WaitAsync(ctk);

        try
        {
            return await action();
        }
        finally
        {
            Operations.Release();
        }
    }

    private static string? Normalize(string? description)
    {
        return string.IsNullOrWhiteSpace(description)
                ? null
                : description.Trim()
            ;
    }
}
