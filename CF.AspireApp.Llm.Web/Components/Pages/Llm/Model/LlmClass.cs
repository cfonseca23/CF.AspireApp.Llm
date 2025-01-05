namespace CF.AspireApp.Llm.Web.Components.Pages.Llm.Model;

public record StreamingMessageAgent(
    string authorName,
    string role,
    string content,
    string modelId,
    string time
);


public record StreamingMessageTool(
    string authorName,
    string role,
    string content,
    string modelId,
    string time
);


public record StreamingMessage(string content, Metadata obj_metadata);

public record Metadata(object? ModelId, object? ChoiceIndex, object? Metadata_, object? Encoding, object? InnerContent, object? Role);

public class ChatCompletionAgent
{
    public string Name { get; set; }
    public string Instructions { get; set; }
    public bool IsActive { get; set; }

    public ChatCompletionAgent(string name, string instructions, bool isActive = true)
    {
        Name = name;
        Instructions = instructions;
        IsActive = isActive;
    }
}
