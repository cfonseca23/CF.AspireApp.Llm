using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using SK.Kernel.Models;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using Microsoft.Extensions.AI;
// ...existing namespaces...

namespace WebPageEmbeddingToQdrant
{
    public interface IWebScraperService
    {
        Task ProcessWebPageAsync(string url);
    }

    public class WebScraperService : IWebScraperService
    {
        private readonly IConfiguration _configuration;

        public WebScraperService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task ProcessWebPageAsync(string url)
        {
            var kernelOptions = _configuration.GetSection(nameof(KernelOptions)).Get<KernelOptions>();

            using var httpClient = new HttpClient();
            var htmlContent = await httpClient.GetStringAsync(url);
            var textContent = ExtractTextFromHtml(htmlContent);

            await UpsertEmbeddingsToQdrantAsync(textContent, url, kernelOptions);
        }

        private string ExtractTextFromHtml(string html)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            htmlDoc.DocumentNode.Descendants()
                .Where(n => n.Name == "script" || n.Name == "style")
                .ToList()
                .ForEach(n => n.Remove());

            var text = htmlDoc.DocumentNode.InnerText;
            var cleanText = HtmlEntity.DeEntitize(text);
            return System.Text.RegularExpressions.Regex.Replace(cleanText, @"\s+", " ").Trim();
        }

        private async Task UpsertEmbeddingsToQdrantAsync(string text, string url, KernelOptions kernelOptions)
        {
            var channel = QdrantChannel.ForAddress(kernelOptions.QdrantClient.Endpoint);
            var grpcClient = new QdrantGrpcClient(channel);
            var client = new QdrantClient(grpcClient);

            const string collectionName = "fringetv_embeddings_1536";

            if (!await CollectionExistsAsync(client, collectionName))
            {
                await client.CreateCollectionAsync(collectionName,
                    new VectorParams { Size = 1536, Distance = Distance.Cosine });
                Console.WriteLine($"Collection '{collectionName}' created.");
            }

            IEmbeddingGenerator<string, Embedding<float>> generator =
                new OllamaEmbeddingGenerator(new Uri(kernelOptions.OllamaAI.Endpoint), kernelOptions.OllamaAI.TextEmbeddingModelName);

            var chunks = SplitTextIntoChunks(text, maxChunkSize: 1000);
            var random = new Random();
            var points = new List<PointStruct>();

            foreach (var (chunk, index) in chunks.Select((value, index) => (value, index)))
            {
                var embeddings = await generator.GenerateAsync(new[] { chunk });

                foreach (var embedding in embeddings)
                {
                    var embeddingArray = embedding.Vector.ToArray();
                    var point = new PointStruct
                    {
                        Id = (ulong)index + (ulong)random.Next(),
                        Vectors = embeddingArray,
                        Payload = {
                            ["url"] = url,
                            ["chunk"] = chunk.Substring(0, Math.Min(chunk.Length, 200)) + "...",
                            ["index"] = index
                        }
                    };
                    points.Add(point);
                }
            }

            await client.UpsertAsync(collectionName, (IReadOnlyList<Qdrant.Client.Grpc.PointStruct>)points);
            Console.WriteLine($"Embeddings for {url} upserted to Qdrant.");
        }

        private async Task<bool> CollectionExistsAsync(QdrantClient client, string collectionName)
        {
            try
            {
                var collectionInfo = await client.GetCollectionInfoAsync(collectionName);
                return collectionInfo != null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking collection existence: {ex.Message}");
                return false;
            }
        }

        private IEnumerable<string> SplitTextIntoChunks(string text, int maxChunkSize)
        {
            for (int i = 0; i < text.Length; i += maxChunkSize)
            {
                yield return text.Substring(i, Math.Min(maxChunkSize, text.Length - i));
            }
        }
    }
}
