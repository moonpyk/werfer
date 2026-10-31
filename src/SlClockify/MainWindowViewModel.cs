using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using Refit;
using SlClockify.Core;
using SlClockify.Core.Models;

namespace SlClockify.Gui;

public class MainWindowViewModel(
    IClockifyApi api, 
    IOptions<ClockifyOptions> options
) : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    // Only written by the TextBox, so no change notification needed
    public string ApiKey { get; set; } = string.Empty;

    public string? Status
    {
        get;
        private set => Set(ref field, value);
    }

    public bool IsBusy
    {
        get;
        private set => Set(ref field, value);
    }

    public async Task ConnectAsync()
    {
        // Same instance the auth handler reads, so the next call uses the new key
        options.Value.ApiKey = ApiKey.Trim();
        IsBusy = true;

        try
        {
            var user = await api.GetCurrentUserAsync();
            Status = $"Connected as {user.Name} ({user.Email})";
        }
        catch (ApiException e) when (e.StatusCode == HttpStatusCode.Unauthorized)
        {
            Status = "Invalid API key";
        }
        catch (Exception e)
        {
            Status = e.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
