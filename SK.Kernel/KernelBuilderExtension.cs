using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using SK.Kernel.Models;
using SK.Kernel.Service;

namespace SK.Kernel;

#pragma warning disable SKEXP0070;
public static class KernelBuilderExtension
{

    public static void AddBrainKernel(this IHostApplicationBuilder builder)
    {
        // Registrar KernelOptions
        builder.Services.Configure<KernelOptions>(builder.Configuration.GetSection(nameof(KernelOptions)));

        // Registrar servicios necesarios
        builder.Services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<KernelOptions>>();
            return new KernelService(options);
        });

        builder.Services.AddSingleton<ChatService>();
        builder.Services.AddSingleton<ToolService>();
        builder.Services.AddSingleton<AgentService>();
        builder.Services.AddSingleton<RAGService>();
    }

    public static void AddBrainKernel(this IServiceCollection services, IConfiguration configuration)
    {
        // Registrar KernelOptions desde la configuración
        services.Configure<KernelOptions>(configuration.GetSection(nameof(KernelOptions)));

        // Registrar servicios necesarios
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<KernelOptions>>();
            return new KernelService(options);
        });

        services.AddSingleton<ChatService>();
        services.AddSingleton<ToolService>();
        services.AddSingleton<AgentService>();
        services.AddSingleton<RAGService>();
    }
    //public static void AddBrainKernel(this IHostApplicationBuilder builder)
    //{
    //    // Registrar KernelOptions
    //    builder.Services.Configure<KernelOptions>(builder.Configuration.GetSection(nameof(KernelOptions)));

    //    // Registrar KernelService
    //    builder.Services.AddSingleton(sp =>
    //    {
    //        var options = sp.GetRequiredService<IOptions<KernelOptions>>();
    //        return new KernelService(options);
    //    });

    //    // Registrar ToolChatCompletionService
    //    builder.Services.AddSingleton(sp =>
    //    {
    //        var options = sp.GetRequiredService<IOptions<KernelOptions>>();
    //        return new ToolChatCompletionService(options);
    //    });

    //    // Registrar ChatService
    //    builder.Services.AddSingleton<ChatService>();

    //    // Registrar ToolService
    //    builder.Services.AddSingleton<ToolService>();

    //    // Registrar AgentService
    //    builder.Services.AddSingleton<AgentService>();

    //    // Registrar RAGService
    //    builder.Services.AddSingleton<RAGService>();
    //}

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
