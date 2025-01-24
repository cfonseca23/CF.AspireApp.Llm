using System.Text.Json;
using System.Runtime.CompilerServices;

using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Plugins.Memory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
//using Microsoft.SemanticKernel.Connectors.OpenAI;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel.Embeddings;
using HtmlAgilityPack;

using SK.Kernel.Plugins;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.Logging;
using SK.Kernel.Memory;
using Google.Protobuf.Collections;
using Microsoft.SemanticKernel.Text;
using SK.Kernel.ModelsRAG;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using System.Text;
using Microsoft.Extensions.Options;

#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0110
#pragma warning disable SKEXP0050
#pragma warning disable SKEXP0070 // Este tipo se incluye solo con fines de evaluación y está sujeto a cambios o a que se elimine en próximas actualizaciones. Suprima este diagnóstico para continuar.


namespace SK.Kernel
{
    public class KernelService
    {
        private readonly Microsoft.SemanticKernel.Kernel _kernel;
        private readonly KernelFunction _function;
        private readonly LocalServerClientHandler _localServerClientHandler;

        public KernelService(IOptions<KernelOptions> options, LocalServerClientHandler localServerClientHandler)
        {
            _localServerClientHandler = localServerClientHandler;
            var kernelOptions = options.Value;
            _kernel = KernelHelper.GetKernelBuilder(kernelOptions).Build();
            _function = _kernel.CreateFunctionFromPrompt(Constants.Prompt);
        }

        public async Task<string> GetChatCompletionResponseAsync(string? userInput, string? history)
        {
            var options = new JsonSerializerOptions();
            ChatHistory? chatHistory = null;

            if (!string.IsNullOrWhiteSpace(history))
            {
                chatHistory = JsonSerializer.Deserialize<ChatHistory?>(history, options);
            }

            ChatCompletionAgent agent = new()
            {
                Name = "AsistenteIAAgent",
                Instructions = "Eres un chat asistente",
                Kernel = _kernel.Clone()
            };

            ChatHistory chat = chatHistory ?? new ChatHistory();

            if (!(chat.Count > 0 && chat.Last().Role == AuthorRole.User))
            {
                userInput ??= "¿en qué puedes ayudarme?";
                chat.AddUserMessage(userInput);
            }

            string responseChat = string.Empty;
            await foreach (var response in agent.InvokeAsync(chat))
            {
                responseChat = response.Content!;
            }

            return responseChat;
        }

        #region Chat with and without history

        public async IAsyncEnumerable<StreamingChatMessageContent>
    GetStreamingChatMessageContentsAsync(string userInput,
                                         ChatHistory? history,
                                         PromptExecutionSettings? executionSettings = null,
                                         [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            history ??= new ChatHistory();
            if (!string.IsNullOrWhiteSpace(userInput))
            {
                history.AddUserMessage(userInput);
            }

            var kernelSimple = _kernel.Clone();
            var chatCompletionService = kernelSimple.GetRequiredService<IChatCompletionService>();
            await foreach (var messageContent in chatCompletionService.GetStreamingChatMessageContentsAsync(history, executionSettings, kernelSimple, cancellationToken))
            {
                yield return messageContent;
            }
        }

        #endregion

        #region tool
        public async IAsyncEnumerable<(string AuthorName, string Role, string Content, string ModelId, DateTime Timestamp)>
GetDetailedStreamingChatMessageToolContentsAsync(string userInput,
                                             ChatHistory? history,
                                             PromptExecutionSettings? executionSettings = null,
                                             [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            history ??= new ChatHistory();
            if (!string.IsNullOrWhiteSpace(userInput))
            {
                history.AddUserMessage(userInput);
            }
            var kerneltool = _kernel.Clone();

            var hostName = "AI Assistant";
            var hostInstructions =
                @"You are a friendly assistant";

            KernelPlugin DateTimePlugin = KernelPluginFactory.CreateFromType<DateTimePlugin>();
            kerneltool.Plugins.Add(DateTimePlugin);

            var settingsOllama = new OllamaPromptExecutionSettings()
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };

            ChatCompletionAgent agent =
                       new()
                       {
                           Instructions = hostInstructions,
                           Name = hostName,
                           Kernel = kerneltool,
                           Arguments = new KernelArguments(settingsOllama),
                           LoggerFactory = kerneltool.Services.GetRequiredService<ILoggerFactory>()
                       };

            AgentGroupChat chat = new();
            chat.AddChatMessages(history.Where(e => !e.Role.ToString().ToUpper().Equals(AuthorRole.System.ToString().ToUpper())).ToList());

            string dateWithTime = await kerneltool.InvokeAsync<string>("DateTimePlugin", "DateWithTime", cancellationToken: cancellationToken);
            Console.WriteLine("dateWithTime: " + dateWithTime);

            await foreach (StreamingChatMessageContent messageContent in chat.InvokeStreamingAsync(agent, cancellationToken: cancellationToken))
            {
                yield return (messageContent.AuthorName, messageContent.Role.ToString(), messageContent.Content, messageContent.ModelId, DateTime.UtcNow);
            }
        }
        #endregion

        #region agent

        public async IAsyncEnumerable<(string AuthorName, string Role, string Content, string ModelId)>
        GetStreamingAgentChatMessageContentsAsync(string userInput,
                                                  ChatHistory? history,
                                                  IEnumerable<ChatCompletionAgent> agents,
                                                  PromptExecutionSettings? executionSettings = null,
                                                  [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            history ??= new ChatHistory();
            if (!string.IsNullOrWhiteSpace(userInput))
            {
                history.AddUserMessage(userInput);
            }

            var kernelJarvis = _kernel.Clone();
            var settings = new OllamaPromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };

            var agentJarvis = new ChatCompletionAgent
            {
                Name = "Jarvis",
                Instructions = "🌟 Eres el agente maestro, siempre listo para liderar con sabiduría y precisión. Responde con un toque futurista y emojis cuando sea apropiado. 🚀",
                Kernel = _kernel.Clone()
            };

            var agentCopilotJarvis = new ChatCompletionAgent
            {
                Name = "CopilotJarvis",
                Instructions = "🤖 Eres el agente copiloto, asistiendo con creatividad y eficiencia. Usa un estilo moderno y añade emojis para hacer las respuestas más dinámicas. ✨",
                Kernel = _kernel.Clone()
            };

            // Asegurarse de que los agentes predeterminados estén al principio
            var agentList = new List<ChatCompletionAgent> { agentJarvis };

            IEnumerable<ChatCompletionAgent> agentsLocal = agentList.Union(agents.Select(agentInfo => new ChatCompletionAgent
            {
                Name = agentInfo.Name,
                Instructions = agentInfo.Instructions,
                Kernel = _kernel.Clone()
            }));

            AgentGroupChat chat = new(agentsLocal.ToArray());

            chat.AddChatMessages(history);

            foreach (var agent in agentsLocal)
            {
                var contentMessage = new StringBuilder();
                string lastAgent = string.Empty;
                await foreach (StreamingChatMessageContent response in chat.InvokeStreamingAsync(agent, cancellationToken: cancellationToken))
                {
                    if (!lastAgent.Equals(response.AuthorName, StringComparison.Ordinal))
                    {
                        lastAgent = response.AuthorName;
                    }
                    contentMessage.Append(response.Content);
                    yield return (response.AuthorName, response.Role.ToString(), response.Content, response.ModelId);
                }

                // Agregar el último mensaje al historial de chat
                chat.AddChatMessage(new ChatMessageContent
                {
                    AuthorName = agent.Name,
                    Role = AuthorRole.Assistant,
                    Content = contentMessage.ToString(),
                    //ModelId = agent.Name, // Asumiendo que el ModelId es el nombre del agente
                    //Timestamp = DateTime.UtcNow
                });
            }
        }


        #endregion

        #region RAG

        public async IAsyncEnumerable<string> ProcessRAGAsync(List<DicDataRag> articles, string userInput)
        {
            var kernelRag = _kernel.Clone();

            KernelPlugin DateTimePlugin = KernelPluginFactory.CreateFromType<DateTimePlugin>();
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

            IEmbeddingGenerator<string, Embedding<float>> generator =
                new Microsoft.Extensions.AI.OllamaEmbeddingGenerator(new Uri("http://localhost:11434/"), "all-minilm");

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

        #endregion



        public async Task<string?> CreateTextEmbeddingAsync(string text)
        {
            var textMemory = _kernel.GetRequiredService<ISemanticTextMemory>();
            string embeddings = await textMemory.SaveInformationAsync(
                collection: Constants.MemoryCollectionName,
                text: text,
                id: Guid.NewGuid().ToString());

            return embeddings;
        }
    }
}
