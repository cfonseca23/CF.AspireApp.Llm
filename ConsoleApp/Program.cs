using ConsoleApp.Utilities;
using ConsoleApp.Services;
using Microsoft.Extensions.AI;

namespace ConsoleApp
{
    class Program
    {
        private const string Url = "https://en.wikipedia.org/wiki/Fringe_(TV_series)";
        private const int VectorSize = 1536;

        static async Task Main(string[] args)
        {
            var httpClient = new HttpClient();
            var qdrantService = new QdrantService("http://localhost:6334", "fringetv_embeddings_1536", VectorSize);
            var embeddingGenerator = new OllamaEmbeddingGenerator(
                new Uri("http://localhost:11434/"),
                "rjmalagon/gte-qwen2-1.5b-instruct-embed-f16:latest"
            );

            try
            {
                var htmlContent = await httpClient.GetStringAsync(Url);
                var textContent = HtmlExtractor.ExtractText(htmlContent);
                await qdrantService.UpsertEmbeddingsAsync(textContent, Url, embeddingGenerator);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }
}
