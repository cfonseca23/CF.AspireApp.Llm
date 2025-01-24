using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SK.Kernel
{
    public static class KernelExtensions
    {
        public static void AddBrainKernel(this IHostApplicationBuilder builder)
        {
            builder.Services.Configure<KernelOptions>(builder.Configuration.GetSection(nameof(KernelOptions)));

            // Registrar KernelService con una fábrica para resolver LocalServerClientHandler
            builder.Services.AddSingleton<KernelService>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<KernelOptions>>();
                var logger = sp.GetRequiredService<ILogger<LocalServerClientHandler>>();
                var localServerClientHandler = new LocalServerClientHandler(options.Value.OllamaAI.Endpoint, logger);
                return new KernelService(options, localServerClientHandler);
            });
        }
    }
}
