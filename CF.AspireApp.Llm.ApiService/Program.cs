using CF.AspireApp.Llm.ApiService.Model;

using Microsoft.AspNetCore.Mvc;

using SK.Kernel;

using System.Text;
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

app.MapPost("/chat", async (KernelService kernelService, string userInput, string? history) =>
{
    var response = await kernelService.GetChatCompletionResponseAsync(userInput, history);
    return Results.Ok(response);
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
