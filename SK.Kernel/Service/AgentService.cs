using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;

using System.Runtime.CompilerServices;

using System.Text;

#pragma warning disable SKEXP0070,SKEXP0110,SKEXP0001

namespace SK.Kernel.Service;

public class AgentService
{
    private readonly KernelService _kernelService;

    public AgentService(KernelService kernelService)
    {
        _kernelService = kernelService;
    }

    public async IAsyncEnumerable<(string AuthorName, string Role, string Content, string ModelId)> GetStreamingAgentChatMessageContentsAsync(string userInput, ChatHistory? history, IEnumerable<ChatCompletionAgent> agents, PromptExecutionSettings? executionSettings = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        history ??= new ChatHistory();
        if (!string.IsNullOrWhiteSpace(userInput))
        {
            history.AddUserMessage(userInput);
        }

        var kernelJarvis = _kernelService.GetKernel();
        var settings = new OllamaPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var agentJarvis = new ChatCompletionAgent
        {
            Name = "Jarvis",
            Instructions = "🌟 Eres el agente maestro, siempre listo para liderar con sabiduría y precisión. Responde con un toque futurista y emojis cuando sea apropiado. 🚀",
            Kernel = _kernelService.GetKernel()
        };

        var agentCopilotJarvis = new ChatCompletionAgent
        {
            Name = "CopilotJarvis",
            Instructions = "🤖 Eres el agente copiloto, asistiendo con creatividad y eficiencia. Usa un estilo moderno y añade emojis para hacer las respuestas más dinámicas. ✨",
            Kernel = _kernelService.GetKernel()
        };

        var agentList = new List<ChatCompletionAgent> { agentJarvis };

        IEnumerable<ChatCompletionAgent> agentsLocal = agentList.Union(agents.Select(agentInfo => new ChatCompletionAgent
        {
            Name = agentInfo.Name,
            Instructions = agentInfo.Instructions,
            Kernel = _kernelService.GetKernel()
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

            chat.AddChatMessage(new ChatMessageContent
            {
                AuthorName = agent.Name,
                Role = AuthorRole.Assistant,
                Content = contentMessage.ToString(),
            });
        }
    }
}