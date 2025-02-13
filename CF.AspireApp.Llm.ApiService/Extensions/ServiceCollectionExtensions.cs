namespace CF.AspireApp.Llm.ApiService.Extensions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

using SK.Kernel.Service;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCustomServices(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddSingleton<KernelService>();
        services.AddSingleton<ToolChatCompletionService>();
        services.AddSingleton<ChatService>();
        services.AddSingleton<AgentService>();
        services.AddSingleton<RAGService>();
        services.AddSingleton<ToolService>();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "AspireApp API", Version = "v1" });
        });

        return services;
    }
}