using Qdrant.Client;
using Qdrant.Client.Grpc;
using Microsoft.Extensions.AI;
using ConsoleApp.Utilities;

namespace ConsoleApp.Services
{
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
            var exists = await _client.CollectionExistsAsync(_collectionName);
            if (exists)
            {
                Console.WriteLine($"Collection '{_collectionName}' already exists. Skipping creation.");
                return;
            }

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
            int index = 0;

            foreach (var chunk in chunks)
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
                index++;
            }

            await _client.UpsertAsync(_collectionName, points);
            Console.WriteLine($"Embeddings for {url} upserted to Qdrant.");
        }
    }
}
