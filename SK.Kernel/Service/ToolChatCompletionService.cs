namespace SK.Kernel.Service;

using System.Runtime.CompilerServices;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SK.Kernel.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel.Agents;
using CF.AspireApp.Llm.ServiceDefaults.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable SKEXP0070, SKEXP0110, SKEXP0001

public class ToolChatCompletionService
{
    private readonly Kernel _kernel;

    public ToolChatCompletionService(IOptions<KernelOptions> options)
    {
        var kernelOptions = options.Value;
        _kernel = KernelBuilderExtension.GetKernelBuilder(kernelOptions).Build().Clone();
    }

    public async IAsyncEnumerable<ChatResponse> GetDetailedStreamingChatMessageToolContentsAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var history = request.History ?? new ChatHistory();
        if (!string.IsNullOrWhiteSpace(request.UserInput))
        {
            history.AddUserMessage(request.UserInput);
        }
        var kerneltool = _kernel.Clone();

        var chatCompletionService = kerneltool.Services.GetRequiredService<IChatCompletionService>();
        var settings = new OllamaPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        ChatMessageContent chatResult = await chatCompletionService.GetChatMessageContentAsync(request.UserInput, settings, kerneltool);

        yield return new ChatResponse(chatResult.AuthorName, chatResult.Role.ToString(), chatResult.Content, chatResult.ModelId, DateTime.UtcNow);
    }
}
