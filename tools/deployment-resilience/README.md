# Deployment resilience probe

Two independent GET probes keep running while you deploy a service manually:
one through the packaging proxy, one directly through downstream CDP ingress.
Each starts one request per second by default, even when earlier requests are
slow. A request passes only with HTTP 200 and the configured `h1` text. Redirects
are disabled and the script adds no retries.

This folder is independent of the .NET solution and application build. It uses
k6 2.3.0 and, for the report, Node.js 18 or newer. Docker Compose provides k6 if
you do not have the CLI installed.

## Run

From this folder:

```sh
cp .env.example .env
mkdir -p results
```

Edit both full URLs in `.env` for the environment under test. The example probes
the public Cookies page without authentication. The proxy path retains
`/manage-recycling-obligations/`; the control path is `/cookies`. Check both
return HTTP 200 and an `h1` of `Cookies` from the machine running the harness.
The machine/container needs network access to both CDP endpoints.

Create a fresh output directory before **each** run:

```sh
resultsDir=$(mktemp -d "results/$(date -u +%Y%m%dT%H%M%SZ)-XXXXXX")
printf 'Output directory: %s\n' "$resultsDir"
RESULTS_DIR="./$resultsDir" docker compose run --rm probe
node summarize.mjs "$resultsDir/requests.jsonl" "$resultsDir/summary.json" > "$resultsDir/report.json"
```

Both commands exit unsuccessfully on any failed request, dropped arrival or
incomplete sample set. Preserve the k6 exit status as well as the report: an
aborted process or invalid configuration may leave no usable summary. The
report has `passed: true` only for a complete run without failures. k6 appends
request logs and overwrites its summary, so reusing a directory mixes runs.
The summarizer rejects mixed logs. Keep each run's directory and deployment notes
together. Compose requires an explicit `RESULTS_DIR` to avoid an accidental
shared output directory.

On Linux, to write files as your own user:

```sh
HARNESS_UID=$(id -u) HARNESS_GID=$(id -g) RESULTS_DIR="./$resultsDir" docker compose run --rm probe
```

With a native k6 CLI, load the same environment and run:

```sh
set -a
. ./.env
set +a
resultsDir=$(mktemp -d "results/$(date -u +%Y%m%dT%H%M%SZ)-XXXXXX")
printf 'Output directory: %s\n' "$resultsDir"
k6 run --log-format=raw --console-output="$resultsDir/requests.jsonl" \
  --summary-export="$resultsDir/summary.json" probe.js
node summarize.mjs "$resultsDir/requests.jsonl" "$resultsDir/summary.json" > "$resultsDir/report.json"
```

Quote values containing spaces in `.env` when loading it through a shell. Without
`--console-output`, request JSON records appear in the console alongside k6
output. Use the file option for the summarizer. k6's normal console summary is
also printed and its metrics are saved in `summary.json`.

## Settings and results

| Setting | Default | Meaning |
| --- | --- | --- |
| `PROXY_URL`, `CONTROL_URL` | Required | Full HTTP(S) URL including the page path; no credentials, query or fragment |
| `EXPECTED_HEADING` | `Cookies` | Exact trimmed `h1` text for both responses |
| `DURATION` | `15m` | Whole seconds/minutes/hours, from 1 second to 1 hour |
| `RATE_PER_TARGET` | `1` | Independent fixed arrival rate per target, 1–10 requests/second |
| `REQUEST_TIMEOUT` | `5s` | Whole seconds/minutes, maximum 30 seconds |
| `MAX_VUS` | `20` | Concurrency ceiling per target, 2–100 |
| `NO_CONNECTION_REUSE` | `false` | Set `true` for a separate experiment opening new client connections |
| `RUN_ID` | Generated | Optional identifier, 1–80 letters/numbers/dots/underscores/hyphens |

The script preallocates enough workers for the configured rate and timeout up
to `MAX_VUS`. Reaching the ceiling can drop arrivals; this fails the run rather
than silently reducing traffic. Each request gets a UUID in `x-cdp-request-id`.
JSONL samples record run ID, global sequence within its target, UTC start/end,
status/error, elapsed wall time, assertion outcomes, returned CDP request ID and
the client peer IP. Response bodies, credentials and cookies are not logged.

The report shows the minimum scheduled count and actual request/failure counts,
maximum and nearest-rank p95 elapsed time, failure counts per UTC second,
each failure's timestamp/request ID, and
the longest failure streak. Streaks use request-start sequence, because parallel
requests can complete out of order. They count failed consecutive probes; they
do not establish the duration of an outage between probes. Missing samples and
dropped iterations are reported separately.
An executor can start one extra request at the duration boundary; this is
allowed. The minimum count and zero-dropped-arrival threshold still detect a
run that does not maintain the requested rate. The report consumes the flattened
JSON format written by k6's `--summary-export` option.

The k6 DNS cache is disabled, and new client connections prefer IPv4 and select
addresses round-robin. Normal connection reuse remains enabled. Repeat with
`NO_CONNECTION_REUSE=true` to compare fresh connections. These settings govern
the harness's connection to ingress; they do not change YARP's DNS refresh or
connection reuse between the proxy and downstream ingress.

## Deployment experiment

1. Start a 15-minute run and collect at least two minutes of baseline traffic.
2. Record a UTC marker, then deploy the frontend normally while the probes run.
3. Record completion and leave at least six minutes of traffic afterwards.
   Increase `DURATION` before starting if the deployment needs longer.
4. Preserve results and deployment revisions, then repeat a few times.
5. Test a proxy deployment in a separate run. Repeat with fresh connections if
   failures appear connection-dependent.

Record markers from another terminal in this folder. Set `resultsDir` to the
output directory printed by the running probe:

```sh
resultsDir=results/20260930T120000Z-ABC123
printf '%s downstream-deployment-start\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)" >> "$resultsDir/markers.txt"
printf '%s downstream-deployment-complete\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)" >> "$resultsDir/markers.txt"
```

Add deployment/image revisions and configured replica counts to those notes.
Compare failures against ingress and application logs using the request IDs.
Failures on both targets point towards the downstream ingress/service; failures
only through the proxy point towards the additional hop. Independent probes may
reach different tasks, so correlate logs before attributing a cause. Verify
traffic reached each proxy replica in the proxy logs; this harness cannot select
individual tasks through an ALB.

This first experiment covers public GET navigation. It does not cover POST,
authentication, business transactions or deployed Azure navigation. Follow up
with the shared journey suite for those paths; a cookie-consent POST needs a
fresh form, retained cookies and a matching CSRF token. No application behaviour
or journey configuration changes are made by adding this tool.

## Identify the IP's hop

For the documented `*.cdp-int.defra.cloud` route, the IP in YARP's outgoing
request URI is expected to belong to downstream CDP ALB ingress. An ingress
NGINX `upstream_addr` is a different hop: the downstream task IP. The sample's
`clientPeerIp` is the harness's ingress peer, not YARP's downstream peer.

From a CDP Terminal/VPC context, compare the DNS sets:

```sh
nslookup waste-obligations-frontend.dev.cdp-int.defra.cloud
nslookup waste-obligations-frontend.dev.public.cdp
curl --noproxy '*' -sS -o /dev/null \
  -w 'peer=%{remote_ip} status=%{http_code}\n' \
  https://waste-obligations-frontend.dev.cdp-int.defra.cloud/health
```

The curl command reports only the selected peer. For definitive ownership,
use the appropriate environment AWS account to inspect the observed private IP:

```sh
aws ec2 describe-network-interfaces --region eu-west-2 \
  --filters Name=addresses.private-ip-address,Values=OBSERVED_IP \
  --query 'NetworkInterfaces[].{id:NetworkInterfaceId,description:Description,ips:PrivateIpAddresses[].PrivateIpAddress}'
```

An `ELB app/...` description identifies an ALB interface. Match ECS task
attachment IPs to distinguish container tasks. The [AWS ALB DNS documentation](https://docs.aws.amazon.com/elasticloadbalancing/latest/application/application-load-balancers.html#dns-name)
describes one IP per enabled availability zone. A three-zone ALB therefore
normally gives three IPv4 DNS answers even with six frontend tasks. A short log
sample may show fewer; addresses accumulated over time may show more as ALB
addresses change. Prove ownership rather than inferring it from the count.

## Local report checks

```sh
node --test summarize.test.mjs
```
