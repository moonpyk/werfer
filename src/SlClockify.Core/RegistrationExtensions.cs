using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Refit;
using SlClockify.Core.Models;

namespace SlClockify.Core;

public static class RegistrationExtensions
{
    [PublicAPI]
    public static IServiceCollection RegisterClockifyClient(
        this IServiceCollection services,
        Action<ClockifyOptions> configureOptions
    )
    {
        services.Configure(configureOptions);

        services.AddTransient<ClockifyAuthHeaderHandler>();

        services.AddRefitClient<IClockifyApi>(new RefitSettings
            {
                UrlParameterFormatter = new ClockifyUrlParameterFormatter(),
                ContentSerializer = new SystemTextJsonContentSerializer(ClockifyJsonContext.Default.Options)
            })
            .ConfigureHttpClient((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<ClockifyOptions>>().Value;
                var baseUri = !string.IsNullOrWhiteSpace(options.BaseUrl)
                    ? options.BaseUrl
                    : ClockifyOptions.DefaultBaseUrl;

                client.BaseAddress = new Uri(baseUri.TrimEnd('/'));
            })
            .AddHttpMessageHandler<ClockifyAuthHeaderHandler>()
            ;

        return services;
    }

    [PublicAPI]
    public static IServiceCollection RegisterClockifyClient(
        this IServiceCollection services,
        string apiKey,
        string? baseUrl = null
    )
    {
        return services.RegisterClockifyClient(options =>
        {
            options.ApiKey = apiKey;
            if (!string.IsNullOrWhiteSpace(baseUrl)) options.BaseUrl = baseUrl;
        });
    }

    public static IServiceCollection RegisterServices(this IServiceCollection services)
    {
        return services;
    }
}
