using CF.AspireApp.Llm.ApiService.Model;

using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel.Agents;

using SK.Kernel;

using System.Text;
using System.Text.Json;
using System.Threading.Channels;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();
builder.AddBrainKernel();

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddSingleton<KernelService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

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


app.MapPost("/stream-agent-chat-history-tool", async (KernelService kernelService, [FromBody] UserInputAgentChatHistoryRequest request, HttpContext context) =>
{
    context.Response.ContentType = "text/event-stream";
    var cancellationToken = context.RequestAborted;

    var channel = Channel.CreateUnbounded<string>();
    var writer = channel.Writer;

    _ = Task.Run(async () =>
    {
        try
        {

            await foreach (var (authorName, role, content, modelId, time) in kernelService.GetDetailedStreamingChatMessageToolContentsAsync(request.UserInput, request.ChatHistory, cancellationToken: cancellationToken))
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



app.MapPost("/stream-agent-chat-history", async (KernelService kernelService, [FromBody] UserInputAgentChatHistoryRequest request, HttpContext context) =>
{
    context.Response.ContentType = "text/event-stream";
    var cancellationToken = context.RequestAborted;

    var channel = Channel.CreateUnbounded<string>();
    var writer = channel.Writer;

    _ = Task.Run(async () =>
    {
        try
        {
#pragma warning disable SKEXP0110
            IEnumerable<ChatCompletionAgent> agents = request.Agents.Select(agentInfo => new ChatCompletionAgent
            {
                Name = agentInfo.Name,
                Instructions = agentInfo.Instructions
            }).ToList();
#pragma warning restore SKEXP0110

            await foreach (var (authorName, role, content, modelId) in kernelService.GetStreamingAgentChatMessageContentsAsync(request.UserInput, request.ChatHistory, agents, cancellationToken: cancellationToken))
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



app.MapPost("/stream-text-chat-history", async (KernelService kernelService, [FromBody] UserInputChatHistoryRequest request, HttpContext context) =>
{
    context.Response.ContentType = "text/event-stream";
    var cancellationToken = context.RequestAborted;

    var channel = Channel.CreateUnbounded<string>();
    var writer = channel.Writer;

    _ = Task.Run(async () =>
    {
        try
        {
            await foreach (var messageContent in kernelService.GetStreamingChatMessageContentsAsync(request.UserInput, request.ChatHistory, cancellationToken: cancellationToken))
            {
                // Construir el JSON estructurado para enviar al cliente
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
app.MapPost("/stream-text", async (KernelService kernelService, [FromBody] UserInputRequest request, HttpContext context) =>
{
    context.Response.ContentType = "text/event-stream";

    var cancellationToken = context.RequestAborted;
    var channel = Channel.CreateUnbounded<string>();
    var writer = channel.Writer;

    _ = Task.Run(async () =>
    {
        try
        {
            await foreach (var messageContent in kernelService.GetStreamingChatMessageContentsAsync(request.UserInput, null, cancellationToken: cancellationToken))
            {
                await writer.WriteAsync(messageContent.Content, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            // Log the error (assuming a logger is available)
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
            break; // Finaliza si el cliente se desconecta
        }

        var data = $"{message}\n\n";
        var bytes = Encoding.UTF8.GetBytes(data);
        await context.Response.Body.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
        await context.Response.Body.FlushAsync(cancellationToken);
    }
});

app.MapDefaultEndpoints();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
