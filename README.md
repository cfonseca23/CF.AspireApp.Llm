LICENSE
Copyright 2025 [Cristhian Fonseca -  cfonseca23]

Todos los derechos reservados. Este software no puede ser usado, modificado, copiado, distribuido ni publicado sin el permiso expreso del propietario.

## Configuración y uso
1. Ajustar parámetros de conexión en appsettings.json, por ejemplo:
   - OllamaConnectionString para la URL del servicio Ollama.
   - RedisConnection para la caché de Redis.
   - KernelOptions para la configuración del modelo, tamaño del vector, etc.

2. Revisar Program.cs, donde se inyectan los servicios:
   - builder.AddServiceDefaults() y builder.AddBrainKernel() para inicializar la aplicación.
   - Se registra KernelService para su uso en controladores o endpoints.

3. Consultar KernelService.cs para ver la lógica principal del asistente, manejo de ChatHistory, uso de agentes y métodos de streaming.

## Endpoints
1. **POST /processRAG**  
   Procesa artículos y genera respuestas con RAG (Retrieval-Augmented Generation).

2. **POST /stream-agent-chat-history-tool**  
   Transmite el contenido de chat detallado con herramientas, útil para obtener metadatos adicionales.

3. **POST /stream-agent-chat-history**  
   Maneja múltiples agentes (Jarvis, CopilotJarvis, etc.) y envía mensajes de manera progresiva.

4. **POST /stream-text-chat-history**  
   Retorna mensajes de chat en streaming, mostrando metadatos y contenido en tiempo real.

5. **POST /stream-text**  
   Envía respuesta textual de forma continua, ideal para manejar respuestas extensas en partes.

## Diagramas Mermaid

### GetChatCompletionResponseAsync
```mermaid
flowchart TD
    A[Recibir userInput, history] --> B[Crear ChatCompletionAgent]
    B --> C[Agregar al ChatHistory]
    C --> D[Invocar agente]
    D --> E[Retornar respuesta final]
```

### GetStreamingChatMessageContentsAsync
```mermaid
flowchart TD
    A[Recibir userInput, history] --> B[Clonar Kernel]
    B --> C[Obtener IChatCompletionService]
    C --> D[Generar mensajes en streaming]
    D --> E[Emitir mensajes a la secuencia]
```

### GetDetailedStreamingChatMessageToolContentsAsync
```mermaid
flowchart TD
    A[Recibir userInput, history] --> B[Clonar Kernel & plugins]
    B --> C[Crear ChatCompletionAgent]
    C --> D[Invocar agente con herramientas]
    D --> E[Transmitir respuesta detallada]
```

### GetStreamingAgentChatMessageContentsAsync
```mermaid
flowchart TD
    A[Recibir userInput, history, lista de agentes] --> B[Crear AgentGroupChat]
    B --> C[Recorrer agentes en secuencia]
    C --> D[Invocar cada agente en streaming]
    D --> E[Devolver respuestas integradas]
```

### ProcessRAGAsync
```mermaid
flowchart TD
    A[Recibir lista de artículos, userInput] --> B[Clonar Kernel]
    B --> C[Generar y almacenar embeddings]
    C --> D[Buscar artículos relevantes]
    D --> E[Invocar Prompt streaming con contexto]
    E --> F[Retornar respuesta RAG]
```

### CreateTextEmbeddingAsync
```mermaid
flowchart TD
    A[Recibir texto] --> B[Obtener ISemanticTextMemory]
    B --> C[Guardar información y generar embedding]
    C --> D[Retornar embeddings]
```

## Relación de Endpoints y Métodos
```mermaid
flowchart LR
    A[/processRAG/] --> B[ProcessRAGAsync]
    C[/stream-agent-chat-history-tool/] --> D[GetDetailedStreamingChatMessageToolContentsAsync]
    E[/stream-agent-chat-history/] --> F[GetStreamingAgentChatMessageContentsAsync]
    G[/stream-text-chat-history/] --> H[GetStreamingChatMessageContentsAsync]
    I[/stream-text/] --> J[GetStreamingChatMessageContentsAsync]
```

## Observaciones de appsettings
- "ConnectionStrings" contiene cadenas de conexión para Ollama, Redis y Qdrant.
- "KernelOptions" define la configuración del modelo de chat y embeddings.
- Ajustar estos valores para apuntar a los servicios correctos y modificar el rendimiento de la IA según sea necesario.
