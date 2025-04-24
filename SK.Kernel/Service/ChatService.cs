using System.Text.Json;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel;
using System.Runtime.CompilerServices;
using Microsoft.SemanticKernel.Agents;
using System.Text.Json.Serialization.Metadata;

#pragma warning disable SKEXP0110

namespace SK.Kernel.Service;

public class ChatService
{
    private readonly KernelService _kernelService;

    public ChatService(KernelService kernelService)
    {
        _kernelService = kernelService;
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
            Kernel = _kernelService.GetKernel()
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

    public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
        string userInput,
        ChatHistory? history,
        PromptExecutionSettings? executionSettings = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        history ??= new ChatHistory();
        if (!string.IsNullOrWhiteSpace(userInput))
        {
            history.AddUserMessage(userInput);
        }

        var kernelSimple = _kernelService.GetKernel();
        var chatCompletionService = kernelSimple.GetRequiredService<IChatCompletionService>();

        //// Configurar JsonSerializerOptions con DefaultJsonTypeInfoResolver
        //var options = new JsonSerializerOptions
        //{
        //    TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        //};

        await foreach (var messageContent in chatCompletionService.GetStreamingChatMessageContentsAsync(history, executionSettings, kernelSimple, cancellationToken))
        {
            yield return messageContent;
        }
    }


}