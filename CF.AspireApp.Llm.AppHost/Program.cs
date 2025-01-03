var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.CF_AspireApp_Llm_ApiService>("apiservice");

builder.AddProject<Projects.CF_AspireApp_Llm_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService);

builder.Build().Run();
