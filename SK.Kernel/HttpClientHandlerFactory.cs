using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using System;

namespace SK.Kernel
{
    public static class HttpClientHandlerFactory
    {
        public static LocalServerClientHandler CreateLocalServerClientHandler(string url, ILogger<LocalServerClientHandler> logger)
        {
            return new LocalServerClientHandler(url, logger);
        }

        public static void ConfigureHttpClient(IServiceCollection services, string baseHost)
        {
            services.AddTransient<LocalServerClientHandler>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<LocalServerClientHandler>>();
                return CreateLocalServerClientHandler(baseHost, logger);
            });

            services.AddHttpClient("OllamaClient")
                .ConfigurePrimaryHttpMessageHandler<LocalServerClientHandler>()
                .ConfigureHttpClient(client =>
                {
                    client.BaseAddress = new Uri(baseHost);
                });
        }
    }
}
