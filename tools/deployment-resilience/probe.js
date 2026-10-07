import http from 'k6/http';
import { check } from 'k6';
import execution from 'k6/execution';
import { Counter, Rate, Trend } from 'k6/metrics';

function integer(name, fallback, minimum, maximum) {
  const value = __ENV[name] || String(fallback);
  if (!/^\d+$/.test(value) || Number(value) < minimum || Number(value) > maximum) {
    throw new Error(`${name} must be an integer from ${minimum} to ${maximum}`);
  }

  return Number(value);
}

function seconds(name, fallback, maximum) {
  const value = __ENV[name] || fallback;
  const match = /^(\d+)(s|m|h)$/.exec(value);
  const result = match && Number(match[1]) * { s: 1, m: 60, h: 3600 }[match[2]];
  if (!result || result > maximum) {
    throw new Error(`${name} must be a whole number of seconds, minutes or hours, from 1s to ${maximum}s`);
  }

  return result;
}

function targetUrl(name) {
  const value = __ENV[name];
  // Require a full path and keep credentials, query strings and fragments out of requests/logs.
  if (!value || !/^https?:\/\/[^\s/?#@]+\/[^\s?#]+$/.test(value)) {
    throw new Error(`${name} must be a full http(s) URL with a non-empty path, without credentials, query or fragment`);
  }

  return value;
}

const urls = { proxy: targetUrl('PROXY_URL'), control: targetUrl('CONTROL_URL') };
const expectedHeading = __ENV.EXPECTED_HEADING === undefined ? 'Cookies' : __ENV.EXPECTED_HEADING;
if (!expectedHeading.trim()) throw new Error('EXPECTED_HEADING must not be empty');
const durationSeconds = seconds('DURATION', '15m', 3600);
const timeoutSeconds = seconds('REQUEST_TIMEOUT', '5s', 30);
const rate = integer('RATE_PER_TARGET', 1, 1, 10);
const maxVUs = integer('MAX_VUS', 20, 2, 100);
const noReuse = __ENV.NO_CONNECTION_REUSE || 'false';
if (!['true', 'false'].includes(noReuse)) throw new Error('NO_CONNECTION_REUSE must be true or false');
if (__ENV.RUN_ID && !/^[a-zA-Z0-9._-]{1,80}$/.test(__ENV.RUN_ID)) {
  throw new Error('RUN_ID must contain 1–80 letters, numbers, dots, underscores or hyphens');
}

const failures = new Rate('probe_failures');
const requests = new Counter('probe_requests');
const elapsed = new Trend('probe_elapsed_ms', true);
const expectedRequests = rate * durationSeconds;
const thresholds = { dropped_iterations: ['count==0'], checks: ['rate==1'] };
const scenarios = {};
for (const target of Object.keys(urls)) {
  scenarios[target] = {
    executor: 'constant-arrival-rate', exec: 'probe', rate, timeUnit: '1s',
    duration: `${durationSeconds}s`, gracefulStop: `${timeoutSeconds + 2}s`,
    preAllocatedVUs: Math.min(maxVUs, Math.ceil(rate * timeoutSeconds) + 1), maxVUs,
    tags: { target },
  };
  thresholds[`probe_failures{target:${target}}`] = ['rate==0'];
  thresholds[`probe_requests{target:${target}}`] = [`count>=${expectedRequests}`];
}

export const options = {
  scenarios, thresholds, maxRedirects: 0,
  dns: { ttl: '0', select: 'roundRobin', policy: 'preferIPv4' },
  noConnectionReuse: noReuse === 'true',
  summaryTrendStats: ['avg', 'p(95)', 'max'],
};

export function setup() {
  const runId = __ENV.RUN_ID || `deployment-${Date.now()}`;
  console.log(JSON.stringify({
    type: 'run', runId, startedAt: new Date().toISOString(),
    durationSeconds, ratePerTarget: rate, minimumRequestsPerTarget: expectedRequests,
    timeoutSeconds, noConnectionReuse: noReuse === 'true',
  }));

  return { runId };
}

function requestId() {
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (character) => {
    const value = Math.floor(Math.random() * 16);

    return (character === 'x' ? value : (value & 3) | 8).toString(16);
  });
}

export function probe({ runId }) {
  const target = execution.scenario.name;
  const sequence = execution.scenario.iterationInTest + 1;
  const id = requestId();
  const started = Date.now();
  const response = http.get(urls[target], {
    timeout: `${timeoutSeconds}s`, redirects: 0,
    headers: { 'x-cdp-request-id': id },
    tags: { target, name: `GET ${target}` },
  });
  const ended = Date.now();
  const statusOk = response.status === 200;
  const contentOk = typeof response.body === 'string'
    && response.html().find('h1').text().trim() === expectedHeading.trim();
  const passed = check(response, {
    'HTTP 200': () => statusOk,
    'expected page content': () => contentOk,
  }, { target });
  failures.add(!passed, { target });
  requests.add(1, { target });
  elapsed.add(ended - started, { target });
  console.log(JSON.stringify({
    type: 'request', runId, target, method: 'GET', sequence, requestId: id,
    startedAt: new Date(started).toISOString(), endedAt: new Date(ended).toISOString(),
    returnedRequestId: response.headers['X-Cdp-Request-Id'] || null,
    clientPeerIp: response.remote_ip || null,
    status: response.status, errorCode: response.error_code || null,
    error: response.error || null,
    elapsedMs: ended - started, passed, statusOk, contentOk,
  }));
}
