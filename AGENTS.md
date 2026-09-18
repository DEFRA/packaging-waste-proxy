# Agent Guidelines

## Coding conventions

- Do not use the `Async` suffix for asynchronous methods.
- Add a blank line before a return statement.
- Use constants for values used more than once; inline values used once.
- Prefer `nameof(EnumType.Member)` over `.ToString()` when converting a known enum member to text.
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
- Run unit tests with `DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=1 dotnet build tests/ReverseProxy.Tests/ReverseProxy.Tests.csproj --no-restore -m:1 -nodeReuse:false --disable-build-servers -v:minimal` followed by `DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=1 dotnet test --test-modules tests/ReverseProxy.Tests/bin/Debug/net10.0/ReverseProxy.Tests.dll --no-build -v:minimal`.
- Run integration tests against the Docker Compose proxy and WireMock downstream: start with `docker compose up --build -d --wait`, run `DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=1 dotnet build tests/ReverseProxy.IntegrationTests/ReverseProxy.IntegrationTests.csproj --no-restore -m:1 -nodeReuse:false --disable-build-servers -v:minimal` followed by `DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=1 dotnet test --test-modules tests/ReverseProxy.IntegrationTests/bin/Debug/net10.0/ReverseProxy.IntegrationTests.dll --no-build -v:minimal`, then stop with `docker compose down -v --remove-orphans`.
- In the sandbox, the test application and Docker Compose need escalation because they bind local sockets and access container services.

## Waste Obligations journey tests

The shared suite lives in
[DEFRA/waste-obligations-journey-tests](https://github.com/DEFRA/waste-obligations-journey-tests).
Read its [run instructions](https://github.com/DEFRA/waste-obligations-journey-tests/blob/main/README.md)
and [agent guidance](https://github.com/DEFRA/waste-obligations-journey-tests/blob/main/AGENTS.md)
when changing behavior used by the journey.

The backend, Waste Obligations frontend and packaging proxy PR workflows use
the [shared action](https://github.com/DEFRA/waste-obligations-journey-tests/blob/main/run-journey-tests/action.yml)
to run E2E, accessibility and passive security profiles against a CDP-only
Docker stack. Browser traffic enters through the packaging proxy; Azure
application navigation is omitted, but the remaining scenario must run.
Azure AD B2C login is still required. After CDP service deployment to dev,
the deployed suite exercises the full Azure-to-CDP journey. Passing the Docker
checks does not prove that deployed Azure navigation or configuration works.

### Coordinating application and journey changes

1. For every application behavior change, assess the shared journey coverage.
   Add or amend scenarios and assertions alongside the application change when
   user-visible behavior, API contracts, authentication, routing or error paths
   change. Record why no journey update is needed when existing coverage suffices.
2. When coordinating changes, create and push the **exact same branch name**
   in `waste-obligations-journey-tests` and every affected application repository
   (`waste-obligations`, `waste-obligations-frontend`, `packaging-waste-proxy`).
   For example, use `MO-123-description` in each changed repository. A local-only
   branch is not visible to CI; do not create empty companion branches where
   no changes are needed.
3. Push companion changes before the validation run. Service PR workflows select
   the matching journey branch, falling back to `main` when absent, and pin the
   calling service to its PR head SHA. For other services, explicit revisions
   take precedence over matching branches; absent both, the action uses published
   images and `main` setup assets where applicable. Check the resolved revisions
   in the run logs: a green fallback run does not validate unpublished changes.
4. A push to a companion repository does not automatically rerun an existing
   service PR check. After all companion changes are pushed, rerun the affected
   service journey jobs (or trigger new runs). Recheck the resolved revisions
   after further companion changes and before merging.
5. Update scenario data and service-owned dependency contracts together with
   assertions. For environment variables and feature flags, follow the
   environment-change checklist below: check CI injection, runner settings and
   deployed service configuration separately. Exercise the applicable Docker
   profiles and the full deployed journey when its environment is available;
   do not skip whole scenarios to conceal missing coverage or configuration.
6. Link companion PRs in each PR description. Record the tested service/test
   revisions, execution mode, results and required configuration changes.
   Describe merge and deployment dependencies explicitly. Keep intermediate
   states compatible, or agree a coordinated rollout before merging; do not
   assume repositories deploy atomically.
7. Matching branches coordinate **PR checks only**. Deployed CDP runs use a
   published journey-test image, not the matching source branch. Before relying
   on post-deployment regression coverage, verify the required journey changes
   are merged, their image is published and the deployed run selects that image.
   Coordinate application deployment, journey-image availability and required
   CDP/Azure configuration; confirm the resulting dev run and its image version.

### Environment and contract changes

For every added, renamed, removed or changed environment variable, feature
flag, default, credential, endpoint or dependency used by this journey:

1. Trace where the service reads the setting and which journey behavior it
   controls. Check the service examples/defaults and deployment configuration.
2. Check the journey repository's
   [CI Compose stack](https://github.com/DEFRA/waste-obligations-journey-tests/blob/main/ci/compose.yml),
   action inputs/environment and caller workflow. Add or amend the value where
   the target service actually receives it; a variable set only on the test
   runner does not configure another container. Update the journey `.env.example`
   only for settings consumed by the runner or required local setup.
3. Review service-owned Compose fragments, WireMock contracts, infrastructure
   initialisers and scenario seed data. Keep service dependency setup with its
   owning service, and shared orchestration/scenario data in the journey repo.
4. Check the deployed CDP and Azure configuration separately. Document required
   flag/secret changes and their owner; Docker values do not propagate there.
   Keep real credentials out of source control and logs.
5. Coordinate repository revisions when contracts change. The three CI callers
   select a matching journey branch or fall back to main; the action resolves
   explicit backend/frontend/proxy revisions, then matching branches, then
   published images with main setup assets. Verify the selected revisions
   contain all required changes before relying on a run.
6. Run the affected Docker journey profiles and the deployed journey where its
   behavior changes and the environment is available. Record mode, revision,
   pass/fail/skip counts and blockers. Explain in the change description which
   journey setup was updated, or why no journey configuration change is needed.
   Do not hide a configuration mismatch by skipping a whole scenario.

This repository provides the application ingress used by the shared CI stack.
For changes to `ReverseProxy__*` destinations, routes, forwarded headers or
authentication, review the proxy container environment in the journey Compose
stack and the deployed routing configuration. Preserve the public
`/manage-recycling-obligations/` path and test browser navigation through the
proxy. The application proxy and CDP outbound/API gateways have different roles.
