# Getting started with Aspire and Dev Containers

Build .NET apps with [Aspire](https://aspire.dev). This repository template provides a ready-to-use development environment for Visual Studio Code Dev Containers and GitHub Codespaces.

## What's included

The container uses an Ubuntu 24.04 base image, with language tooling installed as Dev Container Features:

| Tooling | Purpose |
| --- | --- |
| Aspire CLI and VS Code extension | Orchestrate and debug your app's services |
| Docker-in-Docker | Run containers for databases, caches, and other dependencies |
| .NET 10 SDK, Aspire workload, `dotnet-ef`, and C# Dev Kit | Develop .NET apps and Aspire projects, including EF Core migrations |

VS Code includes the Aspire and C# Dev Kit extensions alongside the .NET tooling.

## Get started

1. [Create a repository from this template](https://github.com/new?template_name=aspire-devcontainer&template_owner=microsoft).
2. Open it in GitHub Codespaces, or clone it and select **Dev Containers: Reopen in Container** in VS Code. Local development requires Docker and the [Dev Containers extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers).
3. Run `aspire new` in the container terminal to choose an app template, or bring your existing services into the repository.

For more guidance, see:

- [Aspire and GitHub Codespaces](https://aspire.dev/get-started/github-codespaces/)
- [Aspire and Visual Studio Code Dev Containers](https://aspire.dev/get-started/dev-containers/)

## Customize your environment

Edit [`.devcontainer/devcontainer.json`](.devcontainer/devcontainer.json) to change language versions, add tooling, or remove features you don't need. Rebuild the container after changing its configuration. The [Dev Container configuration reference](https://containers.dev/implementors/json_reference/) describes the available options.

The template does not run a language-specific restore command. Install your app's dependencies with its package manager, such as `npm install`, `uv sync`, or `dotnet restore`.

### HTMX frontend

The static HTMX frontend uses Tailwind CSS. Before starting or deploying the Aspire app, install its dependencies once:

```sh
cd MyStash.Htmx
npm install
cd ..
```

Aspire watches and rebuilds the Tailwind stylesheet during local runs, and builds the static frontend into `MyStash.Htmx/dist` during deployment.

The container configures HTTPS development certificates at startup through Aspire, independent of your app's language:

```sh
aspire certs trust --non-interactive
```

Aspire stores these certificates under `~/.aspnet/dev-certs/trust`. The container's remote environment includes this directory in `SSL_CERT_DIR` so OpenSSL-based tools such as `curl` can verify HTTPS endpoints from the container terminal.

Trust inside the container does not automatically make your host browser trust the certificate. Follow the [Dev Containers HTTPS guidance](https://aspire.dev/get-started/dev-containers/) for local browser setup.

## Auth0 authentication

Create an Auth0 API and a Regular Web Application. Use the API's issuer URL and identifier, plus the web application's client ID and secret, as Aspire parameters:

```sh
dotnet user-secrets set "Parameters:auth0-authority" "https://YOUR_TENANT.auth0.com/" --project MyStash.AppHost
dotnet user-secrets set "Parameters:auth0-audience" "https://my-stash-api" --project MyStash.AppHost
dotnet user-secrets set "Parameters:auth0-client-id" "YOUR_CLIENT_ID" --project MyStash.AppHost
dotnet user-secrets set "Parameters:auth0-client-secret" "YOUR_CLIENT_SECRET" --project MyStash.AppHost
```

Aspire passes the authority and audience to both services, and passes the client credentials only to the BFF. The BFF uses ASP.NET Core forwarded-header middleware to honor the proxy's `X-Forwarded-Host` and `X-Forwarded-Proto` values when generating Auth0 redirects. Add the public Codespaces frontend URL with `/api/callback` to the Auth0 web application's **Allowed Callback URLs**, and the public frontend origin to **Allowed Logout URLs**. For port `4280`, the URL format is `https://CODESPACE_NAME-4280.GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN`.

Forwarded headers are accepted from known local proxies using ASP.NET Core's default trusted-proxy list; do not clear that list or accept forwarded headers from arbitrary remote clients.

The BFF uses the Authorization Code flow. In Auth0, use a **Regular Web Application** and enable the **Authorization Code** grant. The callback should return a `code` and `state` (including in a form POST); a posted `id_token` indicates an implicit or otherwise mismatched flow.

The BFF uses Auth0 login and a cookie session; it requests an access token for the configured API audience and forwards that token to the Core API. The Core API validates the token. The `/api/products`, `/weatherforecast`, and BFF `/api/fragments/catalog` endpoints require an authenticated user. Use an access token for the API audience, not an ID token. The BFF root, login/logout endpoints, development OpenAPI document, and Aspire health endpoints are explicitly anonymous.

> [!NOTE]
> Once you have created your repository from this template please remember to review the included files such as `LICENSE`, `CODE_OF_CONDUCT.md`, `SECURITY.md` and this `README.md` file to ensure they are appropriate for your circumstances.

## CI checks

The [Devcontainer workflow](.github/workflows/devcontainer.yml) runs on pull requests, pushes to `main`, and manual dispatch. Each matrix job generates a temporary test configuration with extra language tools, then starts the devcontainer and runs [smoke checks](.github/scripts/smoke-test.mjs) before and after stopping and reopening it. These test-only tools are not part of the default .NET development container:

| Scenario | AppHost and services |
| --- | --- |
| Python and React | TypeScript AppHost, FastAPI, React, and Redis; also compiles a .NET console app |
| C# and Blazor | C# AppHost, ASP.NET Core API, Blazor, and Redis |
| TypeScript without .NET SDK | TypeScript AppHost, Express, and React, with the standalone .NET SDK feature omitted |

All jobs check non-root workspace access, tooling, startup certificate trust, Docker-in-Docker, and app endpoints. HTTPS requests verify certificates normally. The SDK-free job checks that `dotnet` is absent from `PATH` before and after running Aspire; Aspire still manages its own bundled .NET components. This is a test-only configuration, not a separate editor preset.

The Redis check uses a non-expiring sentinel value rather than timing-dependent response comparisons. Failures preserve container, AppHost, and resource logs as workflow artifacts. Sample projects stay inside the container. VS Code extensions, debugging, browser rendering, and IDE port forwarding are not covered.

To run the same checks locally with Docker running, first generate the test configuration:

```sh
node .github/scripts/prepare-config.mjs --output /tmp/aspire-devcontainer-test/devcontainer.json
npx --yes --package @devcontainers/cli@0.89.0 devcontainer up --workspace-folder . --config /tmp/aspire-devcontainer-test/devcontainer.json --mount-workspace-git-root false --no-lockfile
npx --yes --package @devcontainers/cli@0.89.0 devcontainer exec --workspace-folder . --config /tmp/aspire-devcontainer-test/devcontainer.json node .github/scripts/smoke-test.mjs
```

The default scenario is `python`; append `csharp` to the smoke command for the C# AppHost. For the SDK-free scenario, generate a temporary configuration with `node .github/scripts/prepare-config.mjs --output /tmp/aspire-no-dotnet/devcontainer.json --without-dotnet`, pass `--config /tmp/aspire-no-dotnet/devcontainer.json` to both CLI commands, and append `typescript-no-dotnet` to the smoke command.

To check restart behavior, stop the container identified by the `up` output, then run both commands again. The smoke script stops its Aspire app; the devcontainer remains running for further use.

## Code of Conduct

This project has adopted the code of conduct defined by the Contributor Covenant
to clarify expected behavior in our community.

For more information, see the [.NET Foundation Code of Conduct](https://dotnetfoundation.org/code-of-conduct).
