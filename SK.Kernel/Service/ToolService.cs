using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.Extensions.Logging;
using SK.Kernel.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using System.Runtime.CompilerServices;

#pragma warning disable SKEXP0070, SKEXP0110, SKEXP0001;

namespace SK.Kernel.Service;

public class ToolService
{
    private readonly KernelService _kernelService;

    public ToolService(KernelService kernelService)
    {
        _kernelService = kernelService;
    }

    public async IAsyncEnumerable<(string AuthorName, string Role, string Content, string ModelId, DateTime Timestamp)> GetDetailedStreamingChatMessageToolContentsAsync(string userInput, ChatHistory? history, PromptExecutionSettings? executionSettings = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        history ??= new ChatHistory();
        if (!string.IsNullOrWhiteSpace(userInput))
        {
            history.AddUserMessage(userInput);
        }
        var kerneltool = _kernelService.GetKernel();

        var hostName = "AI Assistant";
        var hostInstructions = "You are a friendly assistant";

        KernelPlugin DateTimePlugin = KernelPluginFactory.CreateFromType<DateTimePlugin>();
        kerneltool.Plugins.Add(DateTimePlugin);

        var settingsOllama = new OllamaPromptExecutionSettings()
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        ChatCompletionAgent agent = new()
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
}