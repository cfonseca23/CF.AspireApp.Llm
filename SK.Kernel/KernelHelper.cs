using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

using System;
using System.Net.Http;

namespace SK.Kernel
{
    public static class KernelHelper
    {
        public static IKernelBuilder GetKernelBuilder(KernelOptions options)
        {
            var kernelBuilder = Microsoft.SemanticKernel.Kernel.CreateBuilder();
            kernelBuilder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));

            string ollamaEndpoint = options.OllamaAI.Endpoint;
            var baseHost = new Uri(ollamaEndpoint).GetLeftPart(UriPartial.Authority);

            // Configurar HttpClient utilizando LocalServerClientHandler
            HttpClientHandlerFactory.ConfigureHttpClient(kernelBuilder.Services, baseHost);

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
}
