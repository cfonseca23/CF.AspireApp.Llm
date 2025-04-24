using Qdrant.Client;
using HtmlAgilityPack;
using Microsoft.Extensions.AI;
using Qdrant.Client.Grpc;

namespace ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var url = "https://en.wikipedia.org/wiki/Fringe_(TV_series)";
            var httpClient = new HttpClient();
            var qdrantService = new QdrantService("http://localhost:6334", "fringetv_embeddings_1536", 1536);
            var embeddingGenerator = new OllamaEmbeddingGenerator(
                new Uri("http://localhost:11434/"),
                "rjmalagon/gte-qwen2-1.5b-instruct-embed-f16:latest"
            );

            try
            {
                var htmlContent = await httpClient.GetStringAsync(url);
                var textContent = HtmlExtractor.ExtractText(htmlContent);
                await qdrantService.UpsertEmbeddingsAsync(textContent, url, embeddingGenerator);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }

    public static class HtmlExtractor
    {
        public static string ExtractText(string html)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            htmlDoc.DocumentNode.Descendants()
                .Where(n => n.Name == "script" || n.Name == "style")
                .ToList()
                .ForEach(n => n.Remove());

            var text = htmlDoc.DocumentNode.InnerText;
            return CleanText(text);
        }

        private static string CleanText(string text)
        {
            var cleanText = HtmlEntity.DeEntitize(text);
            return System.Text.RegularExpressions.Regex.Replace(cleanText, @"\s+", " ").Trim();
        }
    }

    public class QdrantService
    {
        private readonly QdrantClient _client;
        private readonly string _collectionName;

        public QdrantService(string address, string collectionName, int vectorSize)
        {
            var channel = QdrantChannel.ForAddress(address);
            var grpcClient = new QdrantGrpcClient(channel);
            _client = new QdrantClient(grpcClient);
            _collectionName = collectionName;

            EnsureCollectionExists(vectorSize).Wait();
        }

        private async Task EnsureCollectionExists(int vectorSize)
        {
            // Verificar si la colección ya existe
            var exists = await _client.CollectionExistsAsync(_collectionName);
            if (exists)
            {
                Console.WriteLine($"Collection '{_collectionName}' already exists. Skipping creation.");
                return;
            }

            // Crear la colección si no existe
            await _client.CreateCollectionAsync(_collectionName, new VectorParams
            {
                Size = (ulong)vectorSize,
                Distance = Distance.Cosine
            });
            Console.WriteLine($"Collection '{_collectionName}' created successfully.");
        }

        public async Task UpsertEmbeddingsAsync(string text, string url, IEmbeddingGenerator<string, Embedding<float>> generator)
        {
            var chunks = TextChunker.SplitTextIntoChunks(text, 1000);
            var points = new List<PointStruct>();
            var random = new Random();

            foreach (var (chunk, index) in chunks.Select((value, index) => (value, index)))
            {
                var embeddingsResult = await generator.GenerateAsync(new[] { chunk });

                foreach (var embedding in embeddingsResult)
                {
                    points.Add(new PointStruct
                    {
                        Id = (ulong)index + (ulong)random.Next(),
                        Vectors = embedding.Vector.ToArray(),
                        Payload = {
                            ["url"] = url,
                            ["chunk"] = chunk.Substring(0, Math.Min(chunk.Length, 200)) + "...",
                            ["index"] = index
                        }
                    });
                }
            }

            await _client.UpsertAsync(_collectionName, points);
            Console.WriteLine($"Embeddings for {url} upserted to Qdrant.");
        }
    }

    public static class TextChunker
    {
        public static IEnumerable<string> SplitTextIntoChunks(string text, int maxChunkSize)
        {
            for (int i = 0; i < text.Length; i += maxChunkSize)
            {
                yield return text.Substring(i, Math.Min(maxChunkSize, text.Length - i));
            }
        }
    }
}
