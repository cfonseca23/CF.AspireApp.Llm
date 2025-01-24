using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace SK.Kernel;

#pragma warning disable SKEXP0070 

public static class KernelExtensions
{
    public static void AddBrainKernel(this IHostApplicationBuilder builder)
    {
        builder.Services.AddOptions<KernelOptions>()
            .BindConfiguration(nameof(KernelOptions))
            .ValidateOnStart();

        var options = builder.Configuration.GetSection(nameof(KernelOptions)).Get<KernelOptions>();
        if (options == null)
        {
            throw new InvalidOperationException("KernelOptions configuration is missing or invalid.");
        }

        var kernelBuilder = Microsoft.SemanticKernel.Kernel.CreateBuilder();
        kernelBuilder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Information));

        string ollamaEndpoint = options.OllamaAI.Endpoint;
        var baseHost = new Uri(ollamaEndpoint).GetLeftPart(UriPartial.Authority);

        // Registrar LocalServerClientHandler
        builder.Services.AddTransient<LocalServerClientHandler>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<LocalServerClientHandler>>();
            return new LocalServerClientHandler(baseHost, logger);
        });

        // Configurar HttpClient utilizando LocalServerClientHandler
        builder.Services.AddHttpClient("OllamaClient")
            .ConfigurePrimaryHttpMessageHandler<LocalServerClientHandler>()
            .ConfigureHttpClient(client =>
            {
                client.BaseAddress = new Uri(baseHost);
            });

        var serviceProvider = builder.Services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("OllamaClient");

        kernelBuilder
            .AddOllamaChatCompletion(options.OllamaAI.ChatModelName, httpClient: client);

        kernelBuilder.AddLocalTextEmbeddingGeneration();

        builder.Services.AddTransient((serviceProvider) =>
        {
            return new Microsoft.SemanticKernel.Kernel(serviceProvider);
        });

        string qdrantUrl = options.QdrantClient.Endpoint;
        string qdrantApiKey = options.QdrantClient.ApiKey;

        HttpClient qdrantClient = new()
        {
            BaseAddress = new Uri(qdrantUrl),
            DefaultRequestHeaders = { { "api-key", qdrantApiKey } }
        };

        var kernel = kernelBuilder.Build();
        builder.Services.AddSingleton(kernel);
        builder.Services.AddSingleton<KernelService>();

        // Inyectar las configuraciones como servicios
        builder.Services.AddSingleton(options.OllamaAI);
        builder.Services.AddSingleton(options.QdrantClient);
    }
}
