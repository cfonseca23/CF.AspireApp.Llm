## Configuration and Usage
1. Adjust connection parameters in appsettings.json, for example:
   - OllamaConnectionString for the Ollama service URL.
   - RedisConnection for Redis caching.
   - KernelOptions for setting the model, vector size, etc.

2. Check Program.cs, where the services are injected:
   - builder.AddServiceDefaults() and builder.AddBrainKernel() to initialize the application.
   - KernelService is registered for use in controllers or endpoints.

3. Refer to KernelService.cs for the main assistant logic, handling ChatHistory, agents, and streaming methods.

## Endpoints
1. **POST /processRAG**  
   Processes articles and generates answers using RAG (Retrieval-Augmented Generation).

2. **POST /stream-agent-chat-history-tool**  
   Streams detailed chat content with tools, useful for retrieving additional metadata.

3. **POST /stream-agent-chat-history**  
   Manages multiple agents (Jarvis, CopilotJarvis, etc.) and sends messages progressively.

4. **POST /stream-text-chat-history**  
   Returns streaming chat messages, displaying metadata and content in real time.

5. **POST /stream-text**  
   Sends textual responses continuously, ideal for handling lengthy replies in parts.

## Mermaid Diagrams

### GetChatCompletionResponseAsync
```mermaid
flowchart TD
    A[Receive userInput, history] --> B[Create ChatCompletionAgent]
    B --> C[Add to ChatHistory]
    C --> D[Invoke agent]
    D --> E[Return final answer]
```

### GetStreamingChatMessageContentsAsync
```mermaid
flowchart TD
    A[Receive userInput, history] --> B[Clone Kernel]
    B --> C[Get IChatCompletionService]
    C --> D[Generate streaming messages]
    D --> E[Emit messages to stream]
```

### GetDetailedStreamingChatMessageToolContentsAsync
```mermaid
flowchart TD
    A[Receive userInput, history] --> B[Clone Kernel & plugins]
    B --> C[Create ChatCompletionAgent]
    C --> D[Invoke agent with tools]
    D --> E[Stream detailed response]
```

### GetStreamingAgentChatMessageContentsAsync
```mermaid
flowchart TD
    A[Receive userInput, history, list of agents] --> B[Create AgentGroupChat]
    B --> C[Iterate agents in sequence]
    C --> D[Invoke each agent in streaming mode]
    D --> E[Return integrated responses]
```

### ProcessRAGAsync
```mermaid
flowchart TD
    A[Receive list of articles, userInput] --> B[Clone Kernel]
    B --> C[Generate and store embeddings]
    C --> D[Search relevant articles]
    D --> E[Invoke streaming Prompt with context]
    E --> F[Return RAG response]
```

### CreateTextEmbeddingAsync
```mermaid
flowchart TD
    A[Receive text] --> B[Get ISemanticTextMemory]
    B --> C[Store info and generate embedding]
    C --> D[Return embeddings]
```

## Endpoints and Methods Relationship
```mermaid
flowchart LR
    A[/processRAG/] --> B[ProcessRAGAsync]
    C[/stream-agent-chat-history-tool/] --> D[GetDetailedStreamingChatMessageToolContentsAsync]
    E[/stream-agent-chat-history/] --> F[GetStreamingAgentChatMessageContentsAsync]
    G[/stream-text-chat-history/] --> H[GetStreamingChatMessageContentsAsync]
    I[/stream-text/] --> J[GetStreamingChatMessageContentsAsync]
```

## Notes about appsettings
- "ConnectionStrings" holds the connection URLs for Ollama, Redis, and Qdrant.
- "KernelOptions" defines the chat and embedding model configuration.
- Adjust these values to target the correct services and tweak AI performance as needed.

## LICENSE

LICENSE
© 2025 [Cristhian Fonseca - cfonseca23]

All rights reserved. This software may not be used, modified, copied, distributed, or published without the express permission of the owner.