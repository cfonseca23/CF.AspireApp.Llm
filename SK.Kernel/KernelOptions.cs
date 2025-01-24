using System.ComponentModel.DataAnnotations;

namespace SK.Kernel
{
    public class KernelOptions
    {
        [Required] public required OllamaAIConfig OllamaAI { get; set; }
        [Required] public required OpenAIConfig OpenAI { get; set; }
        [Required] public required QdrantClientConfig QdrantClient { get; set; }
    }

    public class OllamaAIConfig
    {
        [Required] public required string Endpoint { get; set; }
        [Required] public required string ChatModelName { get; set; }
        [Required] public required string TextEmbeddingModelName { get; set; }
    }

    public class OpenAIConfig
    {
        [Required] public required string ApiKey { get; set; }
        [Required] public required string ChatModelName { get; set; }
        [Required] public required string TextEmbeddingModelName { get; set; }
    }

    public class QdrantClientConfig
    {
        [Required] public required string Endpoint { get; set; }
        [Required] public required string ApiKey { get; set; }
    }
}

