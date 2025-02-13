namespace CF.AspireApp.Llm.ApiService.Extensions;

using CF.AspireApp.Llm.ApiService.Model;

using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel.Agents;

using SK.Kernel.Service;

using System.Text;
using System.Text.Json;
using System.Threading.Channels;

public static class MinimalApiExtensions
{
    public static void MapMinimalApis(this WebApplication app)
    {
        var summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        app.MapGet("/weatherforecast", () =>
        {
            var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
                .ToArray();
            return forecast;
        });

        app.MapPost("/processRAG", async (RAGService ragService, [FromBody] ProcessArticlesRequest request, HttpContext context) =>
        {
            context.Response.ContentType = "text/event-stream";

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted, cts.Token);
            var cancellationToken = linkedCts.Token;

            var channel = Channel.CreateUnbounded<string>();
            var writer = channel.Writer;

            _ = Task.Run(async () =>
            {
                try
                {
                    await foreach (var response in ragService.ProcessRAGAsync(request.articles, request.userInput))
                    {
                        var jsonMessage = JsonSerializer.Serialize(new { response });
                        await writer.WriteAsync($"data: {jsonMessage}\n\n", cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error: {ex.Message}");
                    writer.TryComplete(ex);
                }
                finally
                {
                    writer.TryComplete();
                }
            });

            await foreach (var message in channel.Reader.ReadAllAsync(cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var bytes = Encoding.UTF8.GetBytes(message);
                await context.Response.Body.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
                await context.Response.Body.FlushAsync(cancellationToken);
            }
        });

        app.MapPost("/stream-agent-chat-history-tool", async (ToolService toolService, [FromBody] UserInputAgentChatHistoryRequest request, HttpContext context) =>
        {
            context.Response.ContentType = "text/event-stream";
            var cancellationToken = context.RequestAborted;

            var channel = Channel.CreateUnbounded<string>();
            var writer = channel.Writer;

            _ = Task.Run(async () =>
            {
                try
                {
                    await foreach (var (authorName, role, content, modelId, time) in toolService.GetDetailedStreamingChatMessageToolContentsAsync(request.UserInput, request.ChatHistory, cancellationToken: cancellationToken))
                    {
                        var structuredMessage = new
                        {
                            authorName,
                            role,
                            content,
                            modelId,
                            time
                        };

                        var jsonMessage = JsonSerializer.Serialize(structuredMessage);
                        await writer.WriteAsync($"data: {jsonMessage}\n\n", cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error: {ex.Message}");
                    writer.TryComplete(ex);
                }
                finally
                {
                    writer.TryComplete();
                }
            });

            await foreach (var message in channel.Reader.ReadAllAsync(cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var bytes = Encoding.UTF8.GetBytes(message);
                await context.Response.Body.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
                await context.Response.Body.FlushAsync(cancellationToken);
            }
        });

        app.MapPost("/stream-agent-chat-history", async (AgentService agentService, [FromBody] UserInputAgentChatHistoryRequest request, HttpContext context) =>
        {
            context.Response.ContentType = "text/event-stream";
            var cancellationToken = context.RequestAborted;

            var channel = Channel.CreateUnbounded<string>();
            var writer = channel.Writer;

            _ = Task.Run(async () =>
            {
                try
                {
#pragma warning disable SKEXP0110 // Este tipo se incluye solo con fines de evaluación y está sujeto a cambios o a que se elimine en próximas actualizaciones. Suprima este diagnóstico para continuar.
                    IEnumerable<ChatCompletionAgent> agents = request.Agents.Select(agentInfo => new ChatCompletionAgent
                    {
                        Name = agentInfo.Name,
                        Instructions = agentInfo.Instructions
                    }).ToList();
#pragma warning restore SKEXP0110 // Este tipo se incluye solo con fines de evaluación y está sujeto a cambios o a que se elimine en próximas actualizaciones. Suprima este diagnóstico para continuar.

                    await foreach (var (authorName, role, content, modelId) in agentService.GetStreamingAgentChatMessageContentsAsync(
                        request.UserInput,
                        request.ChatHistory,
                        agents,
                        cancellationToken: cancellationToken))
                    {
                        var structuredMessage = new
                        {
                            authorName,
                            role,
                            content,
                            modelId
                        };

                        var jsonMessage = JsonSerializer.Serialize(structuredMessage);
                        await writer.WriteAsync($"data: {jsonMessage}\n\n", cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error: {ex.Message}");
                    writer.TryComplete(ex);
                }
                finally
                {
                    writer.TryComplete();
                }
            });

            await foreach (var message in channel.Reader.ReadAllAsync(cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var bytes = Encoding.UTF8.GetBytes(message);
                await context.Response.Body.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
                await context.Response.Body.FlushAsync(cancellationToken);
            }
        });

        app.MapPost("/stream-text-chat-history", async (ChatService chatService, [FromBody] UserInputChatHistoryRequest request, HttpContext context) =>
        {
            context.Response.ContentType = "text/event-stream";
            var cancellationToken = context.RequestAborted;

            var channel = Channel.CreateUnbounded<string>();
            var writer = channel.Writer;

            _ = Task.Run(async () =>
            {
                try
                {
                    await foreach (var messageContent in chatService.GetStreamingChatMessageContentsAsync(request.UserInput, request.ChatHistory, cancellationToken: cancellationToken))
                    {
                        var structuredMessage = new
                        {
                            content = messageContent.Content,
                            obj_metadata = new
                            {
                                messageContent.ModelId,
                                messageContent.ChoiceIndex,
                                messageContent.Metadata
                            }
                        };

                        var jsonMessage = JsonSerializer.Serialize(structuredMessage);
                        await writer.WriteAsync($"data: {jsonMessage}\n\n", cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error: {ex.Message}");
                    writer.TryComplete(ex);
                }
                finally
                {
                    writer.TryComplete();
                }
            });

            await foreach (var message in channel.Reader.ReadAllAsync(cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var bytes = Encoding.UTF8.GetBytes(message);
                await context.Response.Body.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
                await context.Response.Body.FlushAsync(cancellationToken);
            }
        });

        app.MapPost("/stream-text", async (ChatService chatService, [FromBody] UserInputRequest request, HttpContext context) =>
        {
            context.Response.ContentType = "text/event-stream";

            var cancellationToken = context.RequestAborted;
            var channel = Channel.CreateUnbounded<string>();
            var writer = channel.Writer;

            _ = Task.Run(async () =>
            {
                try
                {
                    await foreach (var messageContent in chatService.GetStreamingChatMessageContentsAsync(request.UserInput, null, cancellationToken: cancellationToken))
                    {
                        await writer.WriteAsync(messageContent.Content, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    writer.TryComplete(ex);
                }
                finally
                {
                    writer.TryComplete();
                }
            }, cancellationToken);

            await foreach (var message in channel.Reader.ReadAllAsync(cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var data = $"{message}\n\n";
                var bytes = Encoding.UTF8.GetBytes(data);
                await context.Response.Body.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
                await context.Response.Body.FlushAsync(cancellationToken);
            }
        });
    }
}
