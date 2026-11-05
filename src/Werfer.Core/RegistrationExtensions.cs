using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
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

            services.AddRefitGeneratedClient<IClockifyApiClient>(new RefitSettings
                {
                    UrlParameterFormatter = new ClockifyUrlParameterFormatter(),
                    ContentSerializer = new SystemTextJsonContentSerializer(WerferCoreJsonContext.Default.Options)
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
            services.AddSingleton<TimeTrackingService>();

            return services;
        }
    }
}
