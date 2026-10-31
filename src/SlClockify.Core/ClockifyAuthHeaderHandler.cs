using Microsoft.Extensions.Options;
using SlClockify.Core.Models;

namespace SlClockify.Core;

public class ClockifyAuthHeaderHandler : DelegatingHandler
{
    private readonly ClockifyOptions _options;

    public ClockifyAuthHeaderHandler(IOptions<ClockifyOptions> options)
    {
        _options = options.Value;
    }

    public ClockifyAuthHeaderHandler(ClockifyOptions options)
    {
        _options = options;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken ctk
    )
    {
        if (!request.Headers.Contains("X-Api-Key"))
        {
            // Checked per call, not at registration: a GUI sets the key after startup
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                throw new InvalidOperationException("Clockify ApiKey is not configured");
            }

            request.Headers.Add("X-Api-Key", _options.ApiKey);
        }

        return base.SendAsync(request, ctk);
    }
}
