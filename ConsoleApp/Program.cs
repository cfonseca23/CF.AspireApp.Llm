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
            // URL de la página web que deseas leer
            var url = "https://en.wikipedia.org/wiki/Fringe_(TV_series)";

            // Crear una instancia de HttpClient
            using var httpClient = new HttpClient();

            try
            {
                // Obtener el contenido de la página web
                var htmlContent = await httpClient.GetStringAsync(url);

                // Analizar y extraer texto del HTML
                var textContent = ExtractTextFromHtml(htmlContent);

                // Generar embeddings a partir del texto extraído y subirlos a Qdrant
                await UpsertEmbeddingsToQdrantAsync(textContent, url);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        // Método para extraer texto del HTML usando HtmlAgilityPack
        static string ExtractTextFromHtml(string html)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            // Eliminar nodos de script y estilo
            htmlDoc.DocumentNode.Descendants()
                .Where(n => n.Name == "script" || n.Name == "style")
                .ToList()
                .ForEach(n => n.Remove());

            // Extraer el texto interno
            var text = htmlDoc.DocumentNode.InnerText;

            // Limpiar el texto
            var cleanText = HtmlEntity.DeEntitize(text);
            cleanText = System.Text.RegularExpressions.Regex.Replace(cleanText, @"\s+", " ").Trim();

            return cleanText;
        }

        // Método para generar embeddings y subirlos a Qdrant
        static async Task UpsertEmbeddingsToQdrantAsync(string text, string url)
        {
            // Crear cliente de Qdrant (sin autenticación, servidor local)
            var channel = QdrantChannel.ForAddress("http://localhost:6334");
            var grpcClient = new QdrantGrpcClient(channel);
            var client = new QdrantClient(grpcClient);

            // Asegurarse de que la colección exista
            await client.CreateCollectionAsync("fringetv_embeddings_1536",
                new VectorParams { Size = 1536, Distance = Distance.Cosine });

            // Generar embeddings usando Ollama
            IEmbeddingGenerator<string, Embedding<float>> generator =
                new OllamaEmbeddingGenerator(new Uri("http://localhost:11434/"), "rjmalagon/gte-qwen2-1.5b-instruct-embed-f16:latest");

            // Dividir el texto en fragmentos para la generación de embeddings
            var chunks = SplitTextIntoChunks(text, maxChunkSize: 1000);
            var random = new Random();
            var points = new List<PointStruct>();

            foreach (var (chunk, index) in chunks.Select((value, index) => (value, index)))
            {
                // Llamar al método GenerateAsync con el chunk envuelto en un array
                var embeddingsResult = await generator.GenerateAsync(new[] { chunk });

                // Acceder a los embeddings generados (ajustar según la estructura de GeneratedEmbeddings<TEmbedding>)
                foreach (var embedding in embeddingsResult)
                {
                    var embeddingArray = embedding.Vector.ToArray();

                    // Crear punto con el vector de embedding y metadatos
                    var point = new PointStruct
                    {
                        Id = (ulong)index + (ulong)random.Next(), // Asegurar IDs únicos
                        Vectors = embeddingArray,
                        Payload = {
                ["url"] = url,
                ["chunk"] = chunk.Substring(0, Math.Min(chunk.Length, 200)) + "...", // Metadatos sobre el fragmento
                ["index"] = index
            }
                    };
                    points.Add(point);
                }
            }


            // Subir los embeddings a Qdrant
            var updateResult = await client.UpsertAsync("fringetv_embeddings_1536", points);
            Console.WriteLine($"Embeddings for {url} upserted to Qdrant.");
        }

        // Método auxiliar para dividir texto en fragmentos manejables
        static IEnumerable<string> SplitTextIntoChunks(string text, int maxChunkSize)
        {
            for (int i = 0; i < text.Length; i += maxChunkSize)
            {
                yield return text.Substring(i, Math.Min(maxChunkSize, text.Length - i));
            }
        }
    }
}
