using Qdrant.Client;
using Qdrant.Client.Grpc;
using Microsoft.Extensions.AI;
using ConsoleApp.Utilities;
using System.Linq;
using System.Collections.Generic;

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

        public static async Task<float[]> GenerateEmbeddingForQueryAsync(string query, IEmbeddingGenerator<string, Embedding<float>> generator)
        {
            var embeddingResult = await generator.GenerateAsync([query]);
            var embedding = embeddingResult.First().Vector.ToArray();
            Console.WriteLine($"Generated embedding for '{query}':");
            Console.WriteLine(string.Join(", ", embedding));
            return embedding;
        }


        public async Task SearchWithPartialMatchAsync(float[] embedding, string searchTerm)
        {
            int limit = 20;
            int offset = 0;
            var allResults = new List<(float Score, string Chunk, string Url, bool Found)>();

            while (true)
            {
                var results = await _client.SearchAsync(
                    _collectionName,
                    embedding,
                    limit: (ulong)limit,
                    offset: (ulong)offset
                );

                if (results == null || !results.Any())
                    break;

                foreach (var result in results)
                {
                    if (result.Payload != null && result.Payload.ContainsKey("chunk"))
                    {
                        string chunk = result.Payload["chunk"].ToString();
                        string url = result.Payload.ContainsKey("url") ? result.Payload["url"].ToString() : "N/A";
                        bool found = chunk.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
                        allResults.Add((result.Score, chunk, url, found));
                    }
                }
                offset += limit;
            }

            var sortedResults = allResults
                .OrderByDescending(r => r.Found)
                .ThenByDescending(r => r.Score)
                .Take(5)
                .ToList();

            if (sortedResults.Count != 0)
            {
                Console.WriteLine($"Top 5 search results for '{searchTerm}' (partial matches allowed):");
                foreach (var res in sortedResults)
                {
                    Console.WriteLine($"Score: {res.Score}");
                    Console.WriteLine($"Chunk: {res.Chunk.ToUpper()}");
                    Console.WriteLine($"URL: {res.Url}");
                    if (res.Found)
                    {
                        Console.WriteLine($"*** Search term '{searchTerm}' found in this chunk! ***");
                    }
                    Console.WriteLine();
                }
            }
            else
            {
                Console.WriteLine($"No relevant results found for '{searchTerm}'.");
            }
        }
    }
}
