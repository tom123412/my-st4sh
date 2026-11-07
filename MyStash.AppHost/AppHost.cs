var builder = DistributedApplication.CreateBuilder(args);

var coreApi = builder.AddProject<Projects.MyStash_CoreApi>("core-api");

var blazorBff = builder.AddProject<Projects.MyStash_BlazorBff>("blazor-bff")
    .WithReference(coreApi)
    ;

var frontendPath = Path.Combine(builder.AppHostDirectory, "../MyStash.Htmx");

var bffEndpoint = blazorBff.GetEndpoint("http");

builder
        .AddExecutable("htmx-frontend", "swa", frontendPath)
        .WithArgs(context =>
        {
            context.Args.Add("start");
            context.Args.Add(".");
            context.Args.Add("--ssl");
            context.Args.Add("--port");
            context.Args.Add("4280");
            context.Args.Add("--api-devserver-url");
            context.Args.Add(ReferenceExpression.Create($"http://127.0.0.1:{bffEndpoint.Property(EndpointProperty.Port)}"));
        })
        .WithReference(blazorBff)
        .WaitFor(blazorBff)
        .WithHttpsEndpoint(port: 4280, targetPort: 4280, isProxied: false, name: "https")
        ;

builder.Build().Run();
