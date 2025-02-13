using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace CF.AspireApp.Llm.ServiceDefaults.Models;

public record ChatRequest(string UserInput,
                          ChatHistory? History,
                          PromptExecutionSettings? ExecutionSettings);
