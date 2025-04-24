using Microsoft.Extensions.DependencyInjection;

using SK.Kernel.Service;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConsoleAppServices(this IServiceCollection services)
    {
        // Registrar servicios específicos para la consola
        services.AddSingleton<KernelService>();
        services.AddSingleton<ChatService>();
        services.AddSingleton<ToolService>();
        services.AddSingleton<AgentService>();
        services.AddSingleton<RAGService>();

        return services;
    }
}