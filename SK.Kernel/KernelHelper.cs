using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

using System;
using System.Net.Http;

namespace SK.Kernel;

public static class KernelHelper
{
    public static IKernelBuilder GetKernelBuilder(KernelOptions options)
    {
        var kernelBuilder = Microsoft.SemanticKernel.Kernel.CreateBuilder();
        kernelBuilder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));

        string ollamaEndpoint = options.OllamaAI.Endpoint;
        var baseHost = new Uri(ollamaEndpoint).GetLeftPart(UriPartial.Authority);

        // Registrar LocalServerClientHandler con la URL en el momento de la creación del kernel
        kernelBuilder.Services.AddTransient<LocalServerClientHandler>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<LocalServerClientHandler>>();
            return new LocalServerClientHandler(baseHost, logger);
        });

        // Configurar HttpClient utilizando LocalServerClientHandler
        kernelBuilder.Services.AddHttpClient("OllamaClient")
            .ConfigurePrimaryHttpMessageHandler<LocalServerClientHandler>()
            .ConfigureHttpClient(client =>
            {
                client.BaseAddress = new Uri(baseHost);
            });

        var serviceProvider = kernelBuilder.Services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("OllamaClient");

#pragma warning disable SKEXP0070 // Este tipo se incluye solo con fines de evaluación y está sujeto a cambios o a que se elimine en próximas actualizaciones. Suprima este diagnóstico para continuar.
        kernelBuilder
            .AddOllamaChatCompletion(options.OllamaAI.ChatModelName, httpClient: client);
#pragma warning restore SKEXP0070 // Este tipo se incluye solo con fines de evaluación y está sujeto a cambios o a que se elimine en próximas actualizaciones. Suprima este diagnóstico para continuar.

        kernelBuilder.AddLocalTextEmbeddingGeneration();

        string qdrantUrl = options.QdrantClient.Endpoint;
        string qdrantApiKey = options.QdrantClient.ApiKey;

        HttpClient qdrantClient = new()
        {
            BaseAddress = new Uri(qdrantUrl),
            DefaultRequestHeaders = { { "api-key", qdrantApiKey } }
        };

        return kernelBuilder;
    }
}
