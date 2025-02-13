using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Plugins;

using SK.Kernel.Data;
using SK.Kernel.Models;
using SK.Kernel.Models.RAG;

using System.Text;

namespace SK.Kernel.Service;

public class RAGService
{
    private readonly KernelService _kernelService;

    public RAGService(KernelService kernelService)
    {
        _kernelService = kernelService;
    }

    public async IAsyncEnumerable<string> ProcessRAGAsync(List<DicDataRag> articles, string userInput)
    {
        var kernelRag = _kernelService.GetKernel();

        KernelPlugin DateTimePlugin = KernelPluginFactory.CreateFromType<Plugins.DateTimePlugin>();
        kernelRag.Plugins.Add(DateTimePlugin);

        var movieData = new List<Movie>()
        {
            new Movie
            {
                Key=0,
                Title="El Rey León",
                Description="El Rey León es una película animada clásica de Disney que cuenta la historia de un joven león llamado Simba que emprende un viaje para reclamar su trono como rey de las Tierras del Reino después de la trágica muerte de su padre."
            },
            new Movie
            {
                Key=1,
                Title="Inception",
                Description="Inception es una película de ciencia ficción dirigida por Christopher Nolan que sigue a un grupo de ladrones que entran en los sueños de sus objetivos para robar información."
            },
            new Movie
            {
                Key=2,
                Title="The Matrix",
                Description="The Matrix es una película de ciencia ficción dirigida por los Wachowski que sigue a un hacker informático llamado Neo que descubre que el mundo en el que vive es una realidad simulada creada por máquinas."
            },
            new Movie
            {
                Key=3,
                Title="Shrek",
                Description="Shrek es una película animada que cuenta la historia de un ogro llamado Shrek que emprende una misión para rescatar a la Princesa Fiona de un dragón y llevarla de vuelta al reino de Duloc."
            }
        };
        var vectorStore = new InMemoryVectorStore();
        var movies = vectorStore.GetCollection<int, Movie>("movies");
        await movies.CreateCollectionIfNotExistsAsync();

        IEmbeddingGenerator<string, Embedding<float>> generator = new OllamaEmbeddingGenerator(new Uri("http://localhost:11434/"), "all-minilm");

        foreach (var movie in movieData)
        {
            movie.Vector = await generator.GenerateEmbeddingVectorAsync(movie.Description);
            await movies.UpsertAsync(movie);
        }

        var queryEmbedding = await generator.GenerateEmbeddingVectorAsync(userInput);
        var searchOptions = new VectorSearchOptions()
        {
            Top = 2,
            VectorPropertyName = "Vector"
        };

        var results = await movies.VectorizedSearchAsync(queryEmbedding, searchOptions);

        var context = new StringBuilder();
        await foreach (var result in results.Results)
        {
            context.AppendLine($"Title: {result.Record.Title}");
            context.AppendLine($"Description: {result.Record.Description}");
            context.AppendLine($"Score: {result.Score}");
            context.AppendLine();
        }
        Console.WriteLine(context.ToString());

        var dicDataRagCollection = vectorStore.GetCollection<string, DicDataRag>("dicDataRag");
        await dicDataRagCollection.CreateCollectionIfNotExistsAsync();

        foreach (var article in articles)
        {
            article.Vector = await generator.GenerateEmbeddingVectorAsync(article.Text);
            await dicDataRagCollection.UpsertAsync(article);
        }

        searchOptions = new VectorSearchOptions()
        {
            Top = 1,
            VectorPropertyName = "Vector"
        };
        var dicDataRagResults = await dicDataRagCollection.VectorizedSearchAsync(queryEmbedding, searchOptions);

        var contextLocal = new StringBuilder();
        contextLocal.AppendLine("Resultados de DicDataRag:");
        await foreach (var result in dicDataRagResults.Results)
        {
            contextLocal.AppendLine($"Id: {result.Record.Id}");
            contextLocal.AppendLine($"Text: {result.Record.Text}");
            contextLocal.AppendLine($"Score: {result.Score}");
            contextLocal.AppendLine();
        }

        Console.WriteLine(contextLocal.ToString());
        var promptChunked = @"Eres un asistente útil que genera consultas de búsqueda "
            + "fecha: {{DateTimePlugin.DateWithTime}}"
            + "TimeZone: {{DateTimePlugin.DateWithTime}}"
            + "basadas en una sola consulta de entrada. "
            + "responder para contestar la pregunta original. "
            + "Si hay acrónimos y palabras que no conoces, no intentes reformularlas. "
            + "Peliculas: {{$context}}"
            + "BD user: {{$contextLocal}}";

        const string MemoryCollectionNameChunked = "originalPrompt";

        OpenAIPromptExecutionSettings settings = new()
        {
            MaxTokens = 100,
            ToolCallBehavior = null,
            Temperature = 0,
            TopP = 0
        };

        var arguments = new KernelArguments(settings)
        {
            { "input", userInput },
            { "collection", MemoryCollectionNameChunked },
            { "context", context.ToString() },
            { "contextLocal", contextLocal.ToString() }
        };

        await foreach (StreamingKernelContent res in kernelRag.InvokePromptStreamingAsync(promptChunked, arguments))
        {
            yield return res.ToString();
        }
        yield return Environment.NewLine + "\n________________________________________________:\n";
        yield return Environment.NewLine + "\n________________________________________________:\n";
        yield return Environment.NewLine + "\n________________________________________________:\n";
        yield return Environment.NewLine + "\nContexto Fijo:\n";
        yield return context.ToString();

        yield return Environment.NewLine + "\nContexto Dinamico:\n";
        yield return contextLocal.ToString();
    }
}