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

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken ctk
    )
    {
        if (
            !string.IsNullOrWhiteSpace(_options.ApiKey) &&
            !request.Headers.Contains("X-Api-Key")
        )
        {
            request.Headers.Add("X-Api-Key", _options.ApiKey);
        }

        return await base.SendAsync(request, ctk);
    }
}
