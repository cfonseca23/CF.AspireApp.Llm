using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using System.Text.Json;

namespace CF.AspireApp.Llm.Web
{
    public class TextStreamApiClient
    {
        private readonly HttpClient _httpClient;

        public TextStreamApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async IAsyncEnumerable<string?> GetLinesAsync(string userInput, [EnumeratorCancellation] CancellationToken cancellationToken = default)
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
    }
}
