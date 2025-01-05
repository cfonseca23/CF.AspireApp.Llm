using Microsoft.SemanticKernel.ChatCompletion;

namespace CF.AspireApp.Llm.ApiService.Model;

public class UserInputRequest
{
    public string UserInput { get; set; }
}


public class UserInputChatHistoryRequest
{
    public string UserInput { get; set; }
    public ChatHistory ChatHistory { get; set; }
}


public class UserInputAgentChatHistoryRequest
{
    public string UserInput { get; set; }
    public ChatHistory? ChatHistory { get; set; }
    public List<AgentInfo> Agents { get; set; }
}

public class AgentInfo
{
    public string Name { get; set; }
    public string Instructions { get; set; }
}