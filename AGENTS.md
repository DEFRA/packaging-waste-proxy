# Agent Guidelines

## Coding conventions

- Do not use the `Async` suffix for asynchronous methods.
- Add a blank line before a return statement.
- Use constants for values used more than once; inline values used once.
- Declare variables as close to their point of use as possible.
- Use camelCase for constants declared within methods.
- Name expressions with `x => x.` syntax where possible.
- Use collection expressions and object initializers where possible.
- Merge related conditionals where doing so keeps the condition clear.
- Prefer `??` directly in a return statement when it clearly expresses a null fallback or exception.
- Use `_camelCase` for private instance fields.
- Prefer AwesomeAssertions for assertions.
- Format changed C# files with `dotnet csharpier format .`.

## Change iterations

- Before adding an endpoint or changing proxy behaviour, compare the nearest existing implementation. If the change needs a one-off request, validation, error-response, or documentation pattern, pause and ask the user before introducing it.
- Keep `GET /health` local to this service. It is a CDP platform health-check contract and must return HTTP 200 with `{ "message": "success" }`.
- Preserve forwarding for all HTTP methods unless a route explicitly restricts them. In particular, do not accidentally exclude `POST` requests.
- Keep `unconfigured.invalid` as a fail-closed destination placeholder. Startup validation must reject it in every configured YARP destination.

## Build and test guidance

- Avoid plain `dotnet build` in the sandbox. Use `DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=1 dotnet build packaging-waste-proxy.slnx --no-restore -m:1 -nodeReuse:false --disable-build-servers -v:minimal`.
- Run unit tests with `DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=1 dotnet test tests/ReverseProxy.Tests/ReverseProxy.Tests.csproj --no-restore -m:1 -nodeReuse:false --disable-build-servers -v:minimal`.
- Run integration tests against the Docker Compose proxy and WireMock downstream: start with `docker compose up --build -d --wait`, run `DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=1 dotnet test tests/ReverseProxy.IntegrationTests/ReverseProxy.IntegrationTests.csproj --no-restore -m:1 -nodeReuse:false --disable-build-servers -v:minimal`, then stop with `docker compose down -v --remove-orphans`.
- In the sandbox, VSTest and Docker Compose need escalation because they bind local sockets and access container services.
