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
                Kernel = _kernel
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

            var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
            await foreach (var messageContent in chatCompletionService.GetStreamingChatMessageContentsAsync(history, executionSettings, _kernel, cancellationToken))
            {
                yield return messageContent;
            }
        }

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

            //KernelFunction[] functions = kerneltool.Plugins
            //    .SelectMany(p => p).ToArray();
            //FunctionChoiceBehavior functionOptions = FunctionChoiceBehavior.Required(functions, true);

            //var settingsOllama = new OllamaPromptExecutionSettings()
            //{
            //    Temperature = 0,
            //    FunctionChoiceBehavior = functionOptions
            //};

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
                           LoggerFactory = _kernel.Services.GetRequiredService<ILoggerFactory>()
                       };
            
            //KernelPlugin DateTimePlugin = KernelPluginFactory.CreateFromType<DateTimePlugin>();
            //agent.Kernel.Plugins.Add(DateTimePlugin);

            AgentGroupChat chat = new();
            chat.AddChatMessages(history.Where(e => !e.Role.ToString().ToUpper().Equals(AuthorRole.System.ToString().ToUpper())).ToList());

            string dateWithTime = await kerneltool.InvokeAsync<string>("DateTimePlugin", "DateWithTime", cancellationToken: cancellationToken);
            Console.WriteLine("dateWithTime: " + dateWithTime);

            await foreach (StreamingChatMessageContent messageContent in chat.InvokeStreamingAsync(agent, cancellationToken: cancellationToken))
            {
                yield return (messageContent.AuthorName, messageContent.Role.ToString(), messageContent.Content, messageContent.ModelId, DateTime.UtcNow);
            }
        }

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

        public async IAsyncEnumerable<string> ProcessArticlesAndAnswerQuestionsAsync(IEnumerable<string> articleUrls,
                                                                                     string userInput)
        {
            var embeddingGenerator = _kernel.Services.GetRequiredService<ITextEmbeddingGenerationService>();
            var memory = new SemanticTextMemory(new VolatileMemoryStore(), embeddingGenerator);

            const string collectionName = "ghc-news";
            var web = new HtmlWeb();

            foreach (var article in articleUrls)
            {
                var htmlDoc = web.Load(article);
                var node = htmlDoc.DocumentNode.Descendants(0).FirstOrDefault(n => n.HasClass("attributed-text-segment-list__content"));
                if (node != null)
                {
                    await memory.SaveInformationAsync(collectionName, node.InnerText, Guid.NewGuid().ToString());
                }
            }

            _kernel.ImportPluginFromObject(new TextMemoryPlugin(memory), "memory");

            var prompt = @"
    Question: {{$input}}
    Answer the question using the memory content: {{Recall}}
    If you don't know an answer, say 'I don't know!'";

            var arguments = new KernelArguments
    {
        { "input", userInput },
        { "collection", collectionName }
    };

            var response = _kernel.InvokePromptStreamingAsync(prompt, arguments);
            await foreach (var res in response)
            {
                yield return res.ToString();
            }
        }

        public IAsyncEnumerable<StreamingKernelContent> GetResponseStreamed(string userInput, ChatHistory? history)
        {
            history ??= new ChatHistory();
            string historyAsString = string.Join(Environment.NewLine + Environment.NewLine, history);

            var arguments = new KernelArguments
            {
                ["history"] = historyAsString,
                ["userInput"] = userInput,
                [TextMemoryPlugin.CollectionParam] = Constants.MemoryCollectionName,
                [TextMemoryPlugin.LimitParam] = "3",
                [TextMemoryPlugin.RelevanceParam] = "0.70"
            };

            return _function.InvokeStreamingAsync(_kernel, arguments);
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
