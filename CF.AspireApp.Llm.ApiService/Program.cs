using CF.AspireApp.Llm.ApiService.Extensions;
using CF.AspireApp.Llm.ApiService.Model;

using Microsoft.OpenApi.Models;

using SK.Kernel;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();
builder.AddBrainKernel();

// Add custom services to the container.
builder.Services.AddCustomServices();

// Add controllers to the container.
builder.Services.AddControllers();

// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    if (!c.SwaggerGeneratorOptions.SwaggerDocs.ContainsKey("v1"))
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "AspireApp API", Version = "v1" });
    }
});


var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

// Enable middleware to serve generated Swagger as a JSON endpoint.
app.UseSwagger();

// Enable middleware to serve ReDoc UI
app.UseReDoc(c =>
{
    c.RoutePrefix = "docs";
    c.SpecUrl = "/swagger/v1/swagger.json";
    c.DocumentTitle = "AspireApp API Documentation";
});

// Map minimal APIs
app.MapMinimalApis();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
