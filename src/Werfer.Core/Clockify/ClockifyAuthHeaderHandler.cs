using Microsoft.Extensions.Options;
using Werfer.Clockify.Models;

namespace Werfer.Clockify;

public class ClockifyAuthHeaderHandler(
    IOptions<ClockifyOptions> options
) : DelegatingHandler
{
    private ClockifyOptions Options { get; } = options.Value;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken ctk
    )
    {
        // ReSharper disable once InvertIf : Clutter
        if (!request.Headers.Contains("X-Api-Key"))
        {
            // Checked per call, not at registration: a GUI sets the key after startup
            if (string.IsNullOrWhiteSpace(Options.ApiKey))
            {
                throw new InvalidOperationException("Clockify ApiKey is not configured");
            }

            request.Headers.Add("X-Api-Key", Options.ApiKey);
        }

        return base.SendAsync(request, ctk);
    }
}
