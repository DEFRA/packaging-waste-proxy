import assert from 'node:assert/strict';
import test from 'node:test';
import { summarize } from './summarize.mjs';

function fixture() {
  const run = { type: 'run', runId: 'test', minimumRequestsPerTarget: 3 };
  const samples = ['proxy', 'control'].flatMap(target => [1, 2, 3].map(sequence => ({
    type: 'request', runId: 'test', target, sequence, requestId: `${target}-${sequence}`,
    startedAt: `2026-09-30T12:00:0${sequence}.000Z`, endedAt: `2026-09-30T12:00:0${sequence}.100Z`,
    elapsedMs: sequence * 100, passed: true, status: 200, statusOk: true, contentOk: true,
  })));
  const summary = { metrics: {
    probe_requests: { count: 6 },
    dropped_iterations: { count: 0, thresholds: { 'count==0': false } },
  } };

  return { run, samples, summary };
}

function report({ run, samples, summary }) {
  return summarize([run, ...samples].map(x => JSON.stringify(x)).join('\n'), summary);
}

test('reports both targets and nearest-rank p95 for a complete successful run', () => {
  const result = report(fixture());
  assert.equal(result.passed, true);
  assert.equal(result.targets.proxy.requests, 3);
  assert.equal(result.targets.proxy.p95ElapsedMs, 300);
});

test('orders failure streaks by global scenario sequence, not completion order', () => {
  const data = fixture();
  data.samples[0].passed = false;
  data.samples[1].passed = false;
  data.samples.reverse();
  const result = report(data);
  assert.equal(result.passed, false);
  assert.equal(result.targets.proxy.longestConsecutiveFailureStreak.count, 2);
  assert.equal(result.targets.proxy.failuresAt[0].requestId, 'proxy-1');
});

test('fails on dropped arrivals, missing samples or failed thresholds', () => {
  const dropped = fixture();
  dropped.summary.metrics.dropped_iterations.count = 1;
  assert.equal(report(dropped).passed, false);
  const missing = fixture();
  missing.samples.pop();
  assert.equal(report(missing).passed, false);
  const threshold = fixture();
  threshold.summary.metrics.dropped_iterations.thresholds['count==0'] = true;
  assert.equal(report(threshold).passed, false);
});

test('rejects concatenated runs, duplicate sequences and missing summaries', () => {
  const data = fixture();
  assert.throws(() => summarize(JSON.stringify(data.run) + '\n' + JSON.stringify(data.run), data.summary));
  data.samples[1].sequence = 1;
  assert.throws(() => report(data), /Duplicate/);
  assert.throws(() => summarize(JSON.stringify(data.run), {}), /No request/);
});

test('accepts a boundary arrival above the minimum in the real flattened export schema', () => {
  const data = fixture();
  data.samples.push({ ...data.samples[2], sequence: 4, requestId: 'proxy-4' });
  data.summary.metrics.probe_requests.count = 7;
  data.summary.metrics['probe_requests{target:proxy}'] = { count: 4, thresholds: { 'count>=3': false } };
  const result = report(data);
  assert.equal(result.passed, true);
  assert.equal(result.minimumRequestsPerTarget, 3);
  assert.equal(result.targets.proxy.requests, 4);
  assert.equal(result.targets.proxy.missingSamples, 0);
});

test('rejects incompatible handleSummary data instead of confusing threshold polarity', () => {
  const data = fixture();
  data.summary.metrics.probe_requests = { values: { count: 6 } };
  assert.throws(() => report(data), /flattened/);
});

test('streaks stop at missing sequence numbers and include the latest overlapping completion', () => {
  const data = fixture();
  data.samples[0].passed = false;
  data.samples[0].endedAt = '2026-09-30T12:00:10.000Z';
  data.samples[1].passed = false;
  const result = report(data);
  assert.equal(result.targets.proxy.longestConsecutiveFailureStreak.lastEndedAt, '2026-09-30T12:00:10.000Z');
  data.samples[1].sequence = 3;
  data.samples[2].sequence = 4;
  assert.equal(report(data).targets.proxy.longestConsecutiveFailureStreak.count, 1);
});
