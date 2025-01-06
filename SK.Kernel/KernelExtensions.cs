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

        var options = builder.Configuration.GetSection(nameof(KernelOptions)).Get<KernelOptions>()!;

        var kernelBuilder = Microsoft.SemanticKernel.Kernel.CreateBuilder();
        kernelBuilder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Information));

        string ollamaEndpoint = builder.Configuration.GetConnectionString(Constants.OllamaConnectionString)!;
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
            //.AddAzureOpenAIChatCompletion(options.ChatModelId, baseHost, "apikey", httpClient: client)
            .AddOllamaChatCompletion(options.ChatModelId, httpClient: client);
            //.AddOllamaTextEmbeddingGeneration(options.TextEmbeddingModelId, new Uri(baseHost));

        kernelBuilder.AddLocalTextEmbeddingGeneration();


        builder.Services.AddTransient((serviceProvider) =>
        {
            return new Microsoft.SemanticKernel.Kernel(serviceProvider);
        });

        string[] qdrantEndpoint = builder.Configuration.GetConnectionString(Constants.QdrantHttpConnectionString)!.Split(";");
        string qdrantUrl = qdrantEndpoint[0]["Endpoint=".Length..];
        string qdrantApiKey = qdrantEndpoint[1]["Key=".Length..];

        HttpClient qdrantClient = new()
        {
            BaseAddress = new Uri(qdrantUrl),
            DefaultRequestHeaders = { { "api-key", qdrantApiKey } }
        };

        var kernel = kernelBuilder.Build();
        builder.Services.AddSingleton(kernel);
        builder.Services.AddSingleton<KernelService>();
    }
}
