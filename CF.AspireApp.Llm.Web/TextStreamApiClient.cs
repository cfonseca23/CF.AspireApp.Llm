using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.ChatCompletion;
using CF.AspireApp.Llm.Web.Components.Pages.Llm.Model;
using System.Reflection;

namespace CF.AspireApp.Llm.Web;

public class TextStreamApiClient
{
    private readonly HttpClient _httpClient;

    public TextStreamApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromMinutes(10);
    }

    #region one shot - una llamada / disparo al llm

    private async IAsyncEnumerable<string?> GetLinesAsync(string userInput, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userInput))
        {
            throw new ArgumentException("User input is required.", nameof(userInput));
        }

        var url = "stream-text";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        request.SetBrowserResponseStreamingEnabled(true);
        request.Content = new StringContent(JsonSerializer.Serialize(new { userInput }), Encoding.UTF8, "application/json");

        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Sending HTTP request to {url}");
        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Response received: {response.StatusCode}");

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Response is successful, starting to read stream");
            using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Response stream obtained");
            using var reader = new StreamReader(responseStream);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Stream opened for reading");

            string? line;
            while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Line read: {line}");
                yield return line;
            }

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] End of stream");
        }
        else
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Server response code: {response.StatusCode}");
            yield return null;
        }
    }

    public async Task StartTextStreamAsync(string userInput, Func<string, Task> onMessageReceived, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(userInput))
        {
            throw new ArgumentException("User input is required.", nameof(userInput));
        }

        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] StartTextStreamAsync started");

        try
        {
            await foreach (var line in GetLinesAsync(userInput, cancellationToken))
            {
                if (!string.IsNullOrEmpty(line))
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Processing line: {line}");
                    await onMessageReceived(line);
                }

                await Task.Yield();
            }

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] End of stream processing");
        }
        catch (Exception ex) when (!(ex is OperationCanceledException))
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Error in StartTextStreamAsync: {ex.Message}");
            throw;
        }
    }

    #endregion

    #region ChatHistory

    private async IAsyncEnumerable<string?> GetLinesWithHistoryAsync(string userInput, ChatHistory chatHistory, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userInput))
        {
            throw new ArgumentException("User input is required.", nameof(userInput));
        }

        var url = "stream-text-chat-history";

        var requestBody = new
        {
            userInput,
            chatHistory
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        request.SetBrowserResponseStreamingEnabled(true);
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Sending HTTP request to {url}");
        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Response received: {response.StatusCode}");

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Response is successful, starting to read stream");
            using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Response stream obtained");
            using var reader = new StreamReader(responseStream);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Stream opened for reading");

            string? line;
            while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
            {
                if (line.StartsWith("data:"))
                {
                    var jsonData = line.Substring(5).Trim(); // Quitar el prefijo "data:"
                    var message = JsonSerializer.Deserialize<StreamingMessage>(jsonData);

                    // Procesar el contenido estructurado
                    if (message != null)
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Message Content: {message.content}");
                        yield return message.content;
                    }
                }
            }

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] End of stream");
        }
        else
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Server response code: {response.StatusCode}");
            yield return null;
        }
    }

    public async Task StartTextStreamWithHistoryAsync(string userInput, ChatHistory chatHistory, Func<string, Task> onMessageReceived, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(userInput))
        {
            throw new ArgumentException("User input is required.", nameof(userInput));
        }

        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] StartTextStreamWithHistoryAsync started");

        try
        {
            await foreach (var line in GetLinesWithHistoryAsync(userInput, chatHistory, cancellationToken))
            {
                if (!string.IsNullOrEmpty(line))
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Processing line: {line}");
                    await onMessageReceived(line);
                }

                await Task.Yield();
            }

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] End of stream processing");
        }
        catch (Exception ex) when (!(ex is OperationCanceledException))
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Error in StartTextStreamWithHistoryAsync: {ex.Message}");
            throw;
        }
    }

    #endregion

    #region agentes

    private async IAsyncEnumerable<StreamingMessageAgent?> GetLinesWithAgentHistoryAsync(string userInput, ChatHistory chatHistory, List<ChatCompletionAgent> agents, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userInput))
        {
            throw new ArgumentException("User input is required.", nameof(userInput));
        }

        var url = "stream-agent-chat-history";

        var requestBody = new
        {
            userInput,
            chatHistory,
            agents
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        request.SetBrowserResponseStreamingEnabled(true);
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Sending HTTP request to {url}");
        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Response received: {response.StatusCode}");

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Response is successful, starting to read stream");
            using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Response stream obtained");
            using var reader = new StreamReader(responseStream);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Stream opened for reading");

            string? line;
            while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
            {
                if (line.StartsWith("data:"))
                {
                    var jsonData = line.Substring(5).Trim(); // Quitar el prefijo "data:"
                    var message = JsonSerializer.Deserialize<StreamingMessageAgent>(jsonData);

                    // Procesar el contenido estructurado
                    if (message != null)
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Message Content: {message.content}");
                        yield return message;
                    }
                }
            }

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] End of stream");
        }
        else
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Server response code: {response.StatusCode}");
            yield return null;
        }
    }

    public async Task StartTextStreamWithAgentHistoryAsync(string userInput, ChatHistory chatHistory, List<ChatCompletionAgent> agents, Func<StreamingMessageAgent, Task> onMessageReceived, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(userInput))
        {
            throw new ArgumentException("User input is required.", nameof(userInput));
        }

        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] StartTextStreamWithAgentHistoryAsync started");

        try
        {
            await foreach (var message in GetLinesWithAgentHistoryAsync(userInput, chatHistory, agents, cancellationToken))
            {
                if (message != null)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Processing message: {message.content}");
                    await onMessageReceived(message);
                }

                await Task.Yield();
            }

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] End of stream processing");
        }
        catch (Exception ex) when (!(ex is OperationCanceledException))
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Error in StartTextStreamWithAgentHistoryAsync: {ex.Message}");
            throw;
        }
    }

    #endregion

    #region tool

    private async IAsyncEnumerable<string?> GetLinesWithHistoryToolAsync(string userInput, ChatHistory chatHistory, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userInput))
        {
            throw new ArgumentException("User input is required.", nameof(userInput));
        }

        var url = "stream-agent-chat-history-tool";

        var requestBody = new
        {
            userInput,
            chatHistory
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        request.SetBrowserResponseStreamingEnabled(true);
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Sending HTTP request to {url}");
        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Response received: {response.StatusCode}");

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Response is successful, starting to read stream");
            using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Response stream obtained");
            using var reader = new StreamReader(responseStream);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Stream opened for reading");

            string? line;
            while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
            {
                if (line.StartsWith("data:"))
                {
                    var jsonData = line.Substring(5).Trim(); // Quitar el prefijo "data:"
                    var message = JsonSerializer.Deserialize<StreamingMessageTool>(jsonData);

                    // Procesar el contenido estructurado
                    if (message != null)
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Message Content: {message.content}");
                        yield return message.content;
                    }
                }
            }

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] End of stream");
        }
        else
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Server response code: {response.StatusCode}");
            yield return null;
        }
    }

    public async Task StartTextStreamWithHistoryToolAsync(string userInput, ChatHistory chatHistory, Func<string, Task> onMessageReceived, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(userInput))
        {
            throw new ArgumentException("User input is required.", nameof(userInput));
        }

        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] StartTextStreamWithHistoryToolAsync started");

        try
        {
            await foreach (var line in GetLinesWithHistoryToolAsync(userInput, chatHistory, cancellationToken))
            {
                if (!string.IsNullOrEmpty(line))
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Processing line: {line}");
                    await onMessageReceived(line);
                }

                await Task.Yield();
            }

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] End of stream processing");
        }
        catch (Exception ex) when (!(ex is OperationCanceledException))
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Error in StartTextStreamWithHistoryToolAsync: {ex.Message}");
            throw;
        }
    }

    #endregion

    #region RAG

    private async IAsyncEnumerable<string?> GetLinesFromArticlesAsync(IList<(string Id, string Text)> articles, string userInput, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userInput))
        {
            throw new ArgumentException("User input is required.", nameof(userInput));
        }

        var url = "processRAG";

        var requestBody = new ProcessArticlesRequest
        {
            articles = articles.Select(a => new DicDataRag { Id = a.Id, Text = a.Text }).ToList(),
            userInput = userInput
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        request.SetBrowserResponseStreamingEnabled(true);
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Sending HTTP request to {url}");
        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, linkedCts.Token);
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Response received: {response.StatusCode}");

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Response is successful, starting to read stream");
            using var responseStream = await response.Content.ReadAsStreamAsync(linkedCts.Token);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Response stream obtained");

            using var reader = new StreamReader(responseStream);
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (line.StartsWith("data:"))
                {
                    var jsonData = line.Substring(5).Trim(); // Quitar el prefijo "data:"
                    var message = JsonSerializer.Deserialize<StreamingMessageRAG>(jsonData);

                    // Procesar el contenido estructurado
                    if (message != null)
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Message Content: {message.response}");
                        yield return message.response;
                    }
                }
            }
        }
        else
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Server response code: {response.StatusCode}");
            yield return null;
        }
    }

    public async Task StartTextStreamFromArticlesAsync(IList<(string Id, string Text)> articles, string userInput, Func<string, Task> onMessageReceived, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(userInput))
        {
            throw new ArgumentException("User input is required.", nameof(userInput));
        }

        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] StartTextStreamFromArticlesAsync started");

        try
        {
            await foreach (var line in GetLinesFromArticlesAsync(articles, userInput, cancellationToken))
            {
                if (!string.IsNullOrEmpty(line))
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Processing line: {line}");
                    await onMessageReceived(line);
                }

                await Task.Yield();
            }

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] End of stream processing");
        }
        catch (Exception ex) when (!(ex is OperationCanceledException))
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Error in StartTextStreamFromArticlesAsync: {ex.Message}");
            throw;
        }
    }

    #endregion
}
