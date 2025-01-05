using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.Memory;

#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0070

namespace SK.Kernel;

/// <summary>
/// Provides extension methods for the <see cref="MemoryBuilder"/> class to configure Ollama connectors.
/// </summary>
public static class OllamaMemoryBuilderExtensions
{
    /// <summary>
    /// Adds ann Ollama text generation service with the specified configuration.
    /// </summary>
    /// <param name="builder">The <see cref="MemoryBuilder"/> instance.</param>
    /// <param name="model">The name of the Ollama model.</param>
    /// <param name="endpoint">The endpoint URL for the text generation service.</param>
    /// <returns>The same instance as <paramref name="builder"/>.</returns>
    public static MemoryBuilder WithOllamaTextEmbeddingGeneration(this MemoryBuilder builder, string model, Uri endpoint)
    {
        return builder.WithTextEmbeddingGeneration((loggerFactory, _) => new OllamaTextEmbeddingGenerationService(model, endpoint, loggerFactory));
    }

    /// <summary>
    /// Adds an Ollama text generation service with the specified configuration.
    /// </summary>
    /// <param name="builder">The <see cref="MemoryBuilder"/> instance.</param>
    /// <param name="model">The name of the Ollama model.</param>
    /// <param name="endpoint">The endpoint URL for the text generation service.</param>
    /// <returns>The same instance as <paramref name="builder"/>.</returns>
    public static MemoryBuilder WithOllamaTextEmbeddingGeneration(this MemoryBuilder builder, string model, string endpoint)
    {
        return WithOllamaTextEmbeddingGeneration(builder, model, new Uri(endpoint));
    }

    /// <summary>
    /// Adds ann Ollama text generation service with the specified configuration.
    /// </summary>
    /// <param name="builder">The <see cref="MemoryBuilder"/> instance.</param>
    /// <param name="model">The name of the Ollama model.</param>
    /// <param name="httpClient">The HttpClient to use with this service.</param>
    /// <returns>The same instance as <paramref name="builder"/>.</returns>
    public static MemoryBuilder WithOllamaTextEmbeddingGeneration(this MemoryBuilder builder, string model, HttpClient httpClient)
    {
        return builder.WithTextEmbeddingGeneration((loggerFactory, _) => new OllamaTextEmbeddingGenerationService(model, httpClient, loggerFactory));
    }
}