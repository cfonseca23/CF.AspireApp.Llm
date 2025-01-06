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


        public KernelService(Microsoft.SemanticKernel.Kernel kernel, LocalServerClientHandler localServerClientHandler)
        {
            _kernel = kernel;
            _function = _kernel.CreateFunctionFromPrompt(Constants.Prompt);
            _localServerClientHandler = localServerClientHandler;
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
            chat.ExecutionSettings.TerminationStrategy.MaximumIterations = 5;

            string lastAgent = string.Empty;
            await foreach (var response in chat.InvokeStreamingAsync(cancellationToken: cancellationToken))
            {
                if (!lastAgent.Equals(response.AuthorName, StringComparison.Ordinal))
                {
                    lastAgent = response.AuthorName;
                }

                yield return (response.AuthorName, response.Role.ToString(), response.Content, response.ModelId);
            }
        }

        #endregion

        public async IAsyncEnumerable<string> ProcessRAGAsync(List<DicDataRag> articles, string userInput)
        {
            var kernelRag = _kernel.Clone();

            var movieData = new List<Movie>()
    {
        new Movie
        {
            Key=0,
            Title="Lion King",
            Description="The Lion King is a classic Disney animated film that tells the story of a young lion named Simba who embarks on a journey to reclaim his throne as the king of the Pride Lands after the tragic death of his father."
        },
        new Movie
        {
            Key=1,
            Title="Inception",
            Description="Inception is a science fiction film directed by Christopher Nolan that follows a group of thieves who enter the dreams of their targets to steal information."
        },
        new Movie
        {
            Key=2,
            Title="The Matrix",
            Description="The Matrix is a science fiction film directed by the Wachowskis that follows a computer hacker named Neo who discovers that the world he lives in is a simulated reality created by machines."
        },
        new Movie
        {
            Key=3,
            Title="Shrek",
            Description="Shrek is an animated film that tells the story of an ogre named Shrek who embarks on a quest to rescue Princess Fiona from a dragon and bring her back to the kingdom of Duloc."
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
                Top = 1,
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
            Console.WriteLine(context.ToString());
            var promptChunked = @"You are a helpful assistant that generates search queries "
                + "based on a single input query. "
                + "Perform query decomposition and break it down into distinct sub questions that you need to "
                + "answer in order to answer the original question "
                + "If there are acronyms and words you are not familiar with, do not try to rephrase them. "
                + "Return sub questions in CSV content. {{$context}}";

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
        { "context", context.ToString() }
    };

            await foreach (StreamingKernelContent res in kernelRag.InvokePromptStreamingAsync(promptChunked, arguments))
            {
                yield return res.ToString();
            }
        }


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
