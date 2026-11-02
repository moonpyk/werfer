using System.Diagnostics;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Refit;
using Werfer.Clockify;
using Werfer.Clockify.Models;
using Werfer.Services;

namespace Werfer;

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
            Action<ClockifyOptions> configure
        )
        {
            services.Configure(configure);

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

        public IServiceCollection RegisterServices()
        {
            services.AddLogging(builder =>
            {
                builder.ClearProviders()
                    // A switch created by name only defaults to Off and lets nothing through
                    .AddTraceSource(new SourceSwitch("slclockify")
                    {
                        Level = SourceLevels.Warning
                    })
#if DEBUG
                    .AddDebug()
#endif
                    ;
            });
            services.AddSingleton<TimeTrackingService>();

            return services;
        }
    }
}
