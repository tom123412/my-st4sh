using System.Diagnostics;
using Aspire.Hosting.Pipelines;
using Azure.Core;
using Azure.Provisioning;
using Azure.Provisioning.AppService;
using Microsoft.Extensions.Logging;

var builder = DistributedApplication.CreateBuilder(args);

// Azure CLI installs Bicep lazily; initialize it once before parallel provisioning steps use it.
#pragma warning disable ASPIREPIPELINES001 // The pipeline API is required to order Bicep initialization before provisioning.
builder.Pipeline.AddStep(
    "ensure-bicep-cli",
    async context =>
    {
        await RunProcessAsync(
            fileName: "az",
            arguments: ["bicep", "install"],
            workingDirectory: builder.AppHostDirectory,
            logger: context.Logger,
            cancellationToken: context.CancellationToken);
    },
    requiredBy: new[]
    {
        "provision-swa-infra",
        "provision-azuresql",
        "provision-my-stash-acr",
        "provision-my-stash",
    });
#pragma warning restore ASPIREPIPELINES001

builder.AddAzureAppServiceEnvironment("my-stash")
    .ConfigureInfrastructure(infra =>
    {
        var plan = infra.GetProvisionableResources().OfType<AppServicePlan>().Single();
        plan.Sku = new AppServiceSkuDescription
        {
            Name = "F1",
            Tier = "Free",
            Capacity = 1,
        };

        var webSites = infra.GetProvisionableResources().OfType<WebSite>();
        foreach (var site in webSites)
        {
            site.SiteConfig.IsAlwaysOn = false;
            site.SiteConfig.NumberOfWorkers = 1;
        }
    })
    ;

var sqlPassword = builder.AddParameter("sql-password", secret: true);
var auth0Authority = builder.AddParameter("auth0-authority");
var auth0Audience = builder.AddParameter("auth0-audience");
var auth0ClientId = builder.AddParameter("auth0-client-id");
var auth0ClientSecret = builder.AddParameter("auth0-client-secret", secret: true);

var azureSql = builder.AddAzureSqlServer("azuresql")
    .RunAsContainer(container =>
    {
        container
            .WithDataVolume() // Persists local dev data across restarts
            .WithPassword(sqlPassword)
            .WithHostPort(14330)
            ;
    })
    ;

var appDb = azureSql.AddDatabase("appdb");

var coreApi = builder.AddProject<Projects.MyStash_CoreApi>("coreapi")
    .WithReference(appDb)
    .WithEnvironment("Auth0__Authority", auth0Authority)
    .WithEnvironment("Auth0__Audience", auth0Audience)
    .WaitFor(appDb)
    .WithExternalHttpEndpoints()
    .PublishAsAzureAppServiceWebsite((infra, website) =>
    {
        website.SiteConfig.IsAlwaysOn = false;
        website.SiteConfig.NumberOfWorkers = 1;
    })
    ;

var blazorBff = builder.AddProject<Projects.MyStash_BlazorBff>("blazor-bff")
    .WithReference(coreApi)
    .WithEnvironment("Auth0__Authority", auth0Authority)
    .WithEnvironment("Auth0__Audience", auth0Audience)
    .WithEnvironment("Auth0__ClientId", auth0ClientId)
    .WithEnvironment("Auth0__ClientSecret", auth0ClientSecret)
    .WaitFor(coreApi)
    .WithExternalHttpEndpoints()
    .PublishAsAzureAppServiceWebsite((infra, website) =>
    {
        website.SiteConfig.IsAlwaysOn = false;
        website.SiteConfig.NumberOfWorkers = 1;
    })
    ;

var frontendPath = Path.Combine(builder.AppHostDirectory, "../MyStash.Htmx");
var bffEndpoint = blazorBff.GetEndpoint("http");

var swaNameOutput = new ProvisioningOutput("swaName", typeof(string));
var swaUrlOutput = new ProvisioningOutput("swaUrl", typeof(string));

var swaInfra = builder.AddAzureInfrastructure("swa-infra", infra =>
{
    var swa = new StaticSite("htmxfrontend")
    {
        Name = "htmx-frontend",
        Location = AzureLocation.EastAsia, // Closest SWA-supported region to Australia (or use WestUS2 / CentralUS)
        Sku = new AppServiceSkuDescription
        {
            Name = "Free",
            Tier = "Free"
        },
        BuildProperties = new StaticSiteBuildProperties
        {
            AppLocation = "./",
        }
    };
    infra.Add(swa);

    swaNameOutput.Value = swa.Name;
    swaUrlOutput.Value = swa.DefaultHostname;
    infra.Add(swaNameOutput);
    infra.Add(swaUrlOutput);
});

builder.AddExecutable("htmx-frontend", "swa", frontendPath)
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
    .WithPipelineStepFactory(factoryContext =>
    {
#pragma warning disable ASPIREPIPELINES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        return new PipelineStep
        {
            Name = "deploy-frontend-swa",
            Description = "Deploys the static SPA to Azure Static Web Apps via SWA CLI.",
            DependsOnSteps = ["provision-swa-infra"], // Waits until Bicep creates the SWA resource
            RequiredBySteps = [WellKnownPipelineSteps.Deploy], // Runs automatically during `aspire deploy`
            Action = async stepContext =>
            {
                var swaNameRef = swaInfra.GetOutput("swaName");
                var swaName = await swaNameRef.GetValueAsync(stepContext.CancellationToken);
                if (string.IsNullOrWhiteSpace(swaName))
                {
                    throw new InvalidOperationException("The Static Web App name was not returned by infrastructure provisioning.");
                }

                var frontendDir = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "../MyStash.Htmx"));

                stepContext.Logger.LogInformation("Deploying SPA from {Dir} to Static Web App '{SwaName}'...", frontendDir, swaName);

                // Run outside the artifact folder; the SWA deploy client rejects a working directory
                // that is identical to or contained within the app artifact folder.
                await RunProcessAsync(
                    fileName: "swa",
                    arguments:
                    [
                        "deploy",
                        frontendDir,
                        "--app-location",
                        frontendDir,
                        "--app-name",
                        swaName,
                        "--env",
                        "production",
                    ],
                    workingDirectory: builder.AppHostDirectory,
                    logger: stepContext.Logger,
                    cancellationToken: stepContext.CancellationToken);
            }
        };
#pragma warning restore ASPIREPIPELINES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    });

builder.Build().Run();


static async Task RunProcessAsync(
    string fileName,
    IReadOnlyList<string> arguments,
    string workingDirectory,
    ILogger logger,
    CancellationToken cancellationToken)
{
    var psi = new ProcessStartInfo
    {
        FileName = fileName,
        WorkingDirectory = workingDirectory,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false
    };

    foreach (var argument in arguments)
    {
        psi.ArgumentList.Add(argument);
    }

    using var process = Process.Start(psi)
        ?? throw new InvalidOperationException($"Failed to start '{fileName}'. Ensure it is installed and on PATH.");

    process.OutputDataReceived += (_, e) => { if (e.Data is not null) logger.ProcessOutput(e.Data); };
    process.ErrorDataReceived += (_, e) => { if (e.Data is not null) logger.LogWarning("{Error}", e.Data); };
    process.BeginOutputReadLine();
    process.BeginErrorReadLine();

    await process.WaitForExitAsync(cancellationToken);

    if (process.ExitCode != 0)
    {
        throw new InvalidOperationException($"Command '{fileName} {string.Join(" ", arguments)}' failed with exit code {process.ExitCode}.");
    }
}

internal static partial class DeployLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "{Output}")]
    public static partial void ProcessOutput(this ILogger logger, string output);
}