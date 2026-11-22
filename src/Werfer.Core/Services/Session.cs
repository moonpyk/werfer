using Werfer.Clockify;
using Werfer.Clockify.Models;
using Werfer.Clockify.ViewModels;

namespace Werfer.Services;

/// <summary>
///     What a connection to Clockify loaded: the user, their workspace and the (project, task) pairs an entry can be
///     started on. Only <see cref="LoadAsync" /> creates one, fully loaded, and it never changes afterwards: a new
///     connection, or a refresh of the projects, loads a new one. Whoever holds a session never sees it half-loaded.
/// </summary>
public sealed class Session
{
    private Session(User user, string workspaceId, IReadOnlyList<ProjectTaskChoice> choices)
    {
        User = user;
        WorkspaceId = workspaceId;
        Choices = choices;
    }

    /// <summary>
    ///     The user the API key belongs to.
    /// </summary>
    public User User { get; }

    /// <summary>
    ///     The user's active workspace, where entries are read and written.
    /// </summary>
    public string WorkspaceId { get; }

    /// <summary>
    ///     Active tasks of the workspace's non-archived projects.
    /// </summary>
    public IReadOnlyList<ProjectTaskChoice> Choices { get; }

    /// <summary>
    ///     Loads the session of the API key <paramref name="client" /> sends: its user, then their workspace's
    ///     projects. Throws if either fails; no session is created then.
    /// </summary>
    public static async Task<Session> LoadAsync(IClockifyApiClient client, CancellationToken ctk = default)
    {
        var user = await client.GetCurrentUserAsync(ctk);
        var workspaceId = user.ActiveWorkspace ?? user.DefaultWorkspace ?? string.Empty;

        var projects = await client.GetProjectsAsync(
            workspaceId,
            false,
            true,
            ctk
        );

        return new Session(
            user,
            workspaceId,
            [
                .. from p in projects
                from pt in p.Tasks ?? []
                where pt.Status == "ACTIVE"
                select new ProjectTaskChoice(p, pt)
            ]
        );
    }
}
