using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Refit;
using SlClockify.Core.Models;
using SlClockify.Core.Services;

namespace SlClockify.Core;

[PublicAPI]
public static class RegistrationExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection RegisterClockifyClient()
        {
            return services.RegisterClockifyClient(_ => { });
        }

        public IServiceCollection RegisterClockifyClient(
            Action<ClockifyOptions> configureOptions
        )
        {
            services.Configure(configureOptions);

            services.AddTransient<ClockifyAuthHeaderHandler>();

            services.AddRefitClient<IClockifyApiClient>(new RefitSettings
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

        public IServiceCollection RegisterClockifyClient(string apiKey,
            string? baseUrl = null
        )
        {
            return services.RegisterClockifyClient(options =>
            {
                options.ApiKey = apiKey;
                if (!string.IsNullOrWhiteSpace(baseUrl))
                {
                    options.BaseUrl = baseUrl;
                }
            });
        }

        public IServiceCollection RegisterServices()
        {
            services.AddSingleton<TimeTrackingService>();

            return services;
        }
    }
}
