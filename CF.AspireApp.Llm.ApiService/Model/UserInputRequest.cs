using Microsoft.SemanticKernel.ChatCompletion;

namespace CF.AspireApp.Llm.ApiService.Model;

public class UserInputRequest
{
    public string UserInput { get; set; }
}


public class UserInputChatHistoryRequest
{
    public string UserInput { get; set; }
    public ChatHistory chatHistory { get; set; }
}

