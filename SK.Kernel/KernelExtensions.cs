using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

using SK.Kernel.Models;
using SK.Kernel.Service;

#pragma warning disable SKEXP0070

namespace SK.Kernel;

public static class KernelExtensions
{
    public static void AddBrainKernel(this IHostApplicationBuilder builder)
    {
        // Registrar KernelOptions
        builder.Services.Configure<KernelOptions>(builder.Configuration.GetSection(nameof(KernelOptions)));

        // Registrar KernelService
        builder.Services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<KernelOptions>>();
            return new KernelService(options);
        });
    }

    public static IKernelBuilder GetKernelBuilder(KernelOptions options)
    {
        var builder = Microsoft.SemanticKernel.Kernel.CreateBuilder();
        builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));

        var serviceProvider = builder.Services.BuildServiceProvider();

        builder
            .AddOllamaChatCompletion(
                options.OllamaAI.ChatModelName, 
                httpClient: CreateHttpClient(serviceProvider, options.OllamaAI.Endpoint));

        builder.AddLocalTextEmbeddingGeneration();

        string qdrantUrl = options.QdrantClient.Endpoint;
        string qdrantApiKey = options.QdrantClient.ApiKey;

        HttpClient qdrantClient = new()
        {
            BaseAddress = new Uri(qdrantUrl),
            DefaultRequestHeaders = { { "api-key", qdrantApiKey } }
        };

        return builder;
    }

    private static HttpClient CreateHttpClient(IServiceProvider serviceProvider, string baseHost)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<LocalServerClientHandler>>();
        var handler = new LocalServerClientHandler(baseHost, logger);

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri(new Uri(baseHost).GetLeftPart(UriPartial.Authority))
        };

        return client;
    }
}
