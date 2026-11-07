var builder = DistributedApplication.CreateBuilder(args);

var coreApi = builder.AddProject<Projects.MyStash_CoreApi>("core-api");

var blazorBff = builder.AddProject<Projects.MyStash_BlazorBff>("blazor-bff")
    .WithReference(coreApi)
    ;


var frontendPath = Path.Combine(builder.AppHostDirectory, "../MyStash.Htmx");

builder.AddExecutable("htmx-frontend", "dotnet-serve", frontendPath, ["-p", "5000"])
       .WithReference(blazorBff)
       .WithHttpEndpoint(targetPort: 5000, name: "http")
       ;

builder.Build().Run();
