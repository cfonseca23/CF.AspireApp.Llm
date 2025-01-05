using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Plugins.Memory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using SK.Kernel;
using System.Runtime.CompilerServices;

#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0110
#pragma warning disable SKEXP0050

namespace SK.Kernel
{
    public class KernelService
    {
        private readonly Microsoft.SemanticKernel.Kernel _kernel;
        private readonly KernelFunction _function;

        public KernelService(Microsoft.SemanticKernel.Kernel kernel)
        {
            _kernel = kernel;
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

        public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(string userInput, ChatHistory? history, PromptExecutionSettings? executionSettings = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
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

        public async IAsyncEnumerable<(string Role, string Content, string ModelId)> GetStreamingAgentChatMessageContentsAsync(string userInput, ChatHistory? history, IEnumerable<ChatCompletionAgent> agents, PromptExecutionSettings? executionSettings = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            history ??= new ChatHistory();
            if (!string.IsNullOrWhiteSpace(userInput))
            {
                history.AddUserMessage(userInput);
            }

            IEnumerable<ChatCompletionAgent> agentsLocal = agents.Select(agentInfo => new ChatCompletionAgent
            {
                Name = agentInfo.Name,
                Instructions = agentInfo.Instructions,
                Kernel = _kernel
            });

            AgentGroupChat chat = new(agentsLocal.ToArray());

            chat.AddChatMessages(history);
            chat.ExecutionSettings.TerminationStrategy.MaximumIterations = 10;

            string lastAgent = string.Empty;
            await foreach (var response in chat.InvokeStreamingAsync(cancellationToken: cancellationToken))
            {
                if (!lastAgent.Equals(response.AuthorName, StringComparison.Ordinal))
                {
                    lastAgent = response.AuthorName;
                }

                yield return (response.Role.ToString(), response.Content, response.ModelId);
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
