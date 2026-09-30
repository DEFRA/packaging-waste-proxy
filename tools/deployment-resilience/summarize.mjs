import { readFileSync } from 'node:fs';
import { pathToFileURL } from 'node:url';

export function summarize(log, summary) {
  const records = log.split(/\r?\n/).filter(x => x.trim()).map(x => JSON.parse(x));
  const runs = records.filter(x => x.type === 'run');
  if (runs.length !== 1) throw new Error('Expected one run record; use a separate file for each run');
  const run = runs[0];
  const minimumRequests = run.minimumRequestsPerTarget ?? run.expectedRequestsPerTarget;
  const samples = records.filter(x => x.type === 'request');
  if (!samples.length) throw new Error('No request samples found');
  if (!Number.isSafeInteger(minimumRequests) || minimumRequests < 1) {
    throw new Error('Run record has no valid expected request count');
  }
  for (const sample of samples) {
    if (sample.runId !== run.runId || !['proxy', 'control'].includes(sample.target)
      || !Number.isSafeInteger(sample.sequence) || sample.sequence < 1
      || !Number.isFinite(sample.elapsedMs) || sample.elapsedMs < 0
      || !Number.isFinite(Date.parse(sample.startedAt)) || !Number.isFinite(Date.parse(sample.endedAt))
      || typeof sample.passed !== 'boolean') {
      throw new Error('Invalid or mixed-run request sample');
    }
  }
  if (!summary.metrics?.probe_requests || !summary.metrics?.dropped_iterations) {
    throw new Error('Missing k6 completion metrics; preserve the summary from the same completed run');
  }
  // k6 --summary-export uses the legacy flattened schema. Threshold booleans
  // indicate failure (true), unlike handleSummary's { ok: true } schema.
  const dropped = summary.metrics.dropped_iterations.count;
  const completedRequests = summary.metrics.probe_requests.count;
  if (!Number.isSafeInteger(dropped) || !Number.isSafeInteger(completedRequests)) {
    throw new Error('Expected the flattened k6 --summary-export format');
  }
  const thresholdFailures = Object.entries(summary.metrics).flatMap(([metric, data]) =>
    Object.entries(data.thresholds || {}).filter(([, failed]) => failed !== false)
      .map(([threshold]) => `${metric}: ${threshold}`));
  const report = {
    runId: run.runId, startedAt: run.startedAt,
    minimumRequestsPerTarget: minimumRequests,
    droppedIterations: dropped, thresholdFailures,
    completeLog: completedRequests === samples.length,
    targets: {},
  };
  for (const target of ['proxy', 'control']) {
    // Completion order can differ from arrival order when several requests are in flight.
    const rows = samples.filter(x => x.target === target).sort((a, b) => a.sequence - b.sequence);
    if (new Set(rows.map(x => x.sequence)).size !== rows.length) throw new Error(`Duplicate ${target} sequence`);
    const latencies = rows.map(x => x.elapsedMs).sort((a, b) => a - b);
    let streak = 0;
    let longest = { count: 0, firstStartedAt: null, lastEndedAt: null };
    let first = null;
    let lastEnd = 0;
    let previousSequence = 0;
    for (const row of rows) {
      if (row.sequence !== previousSequence + 1) streak = 0;
      previousSequence = row.sequence;
      if (row.passed) {
        streak = 0;
      } else {
        if (streak === 0) {
          first = row.startedAt;
          lastEnd = Date.parse(row.endedAt);
        } else {
          lastEnd = Math.max(lastEnd, Date.parse(row.endedAt));
        }
        streak++;
        if (streak > longest.count) {
          longest = { count: streak, firstStartedAt: first, lastEndedAt: new Date(lastEnd).toISOString() };
        }
      }
    }
    const failedRows = rows.filter(x => !x.passed);
    const failuresBySecond = {};
    for (const row of failedRows) {
      const second = row.startedAt.slice(0, 19) + 'Z';
      failuresBySecond[second] = (failuresBySecond[second] || 0) + 1;
    }
    report.targets[target] = {
      requests: rows.length, failures: failedRows.length,
      missingSamples: Math.max(0, minimumRequests - rows.length),
      p95ElapsedMs: latencies.length ? latencies[Math.ceil(latencies.length * 0.95) - 1] : null,
      maxElapsedMs: latencies.length ? latencies.at(-1) : null,
      longestConsecutiveFailureStreak: longest, failuresBySecond,
      failuresAt: failedRows.map(x => ({
        sequence: x.sequence, startedAt: x.startedAt, endedAt: x.endedAt,
        requestId: x.requestId, returnedRequestId: x.returnedRequestId,
        status: x.status, errorCode: x.errorCode, error: x.error,
        statusOk: x.statusOk, contentOk: x.contentOk, elapsedMs: x.elapsedMs,
      })),
    };
  }
  report.passed = report.completeLog && dropped === 0 && thresholdFailures.length === 0
    && Object.values(report.targets).every(x => x.failures === 0 && x.missingSamples === 0);

  return report;
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  try {
    if (process.argv.length !== 4) throw new Error('Usage: node summarize.mjs requests.jsonl summary.json');
    const report = summarize(readFileSync(process.argv[2], 'utf8'), JSON.parse(readFileSync(process.argv[3], 'utf8')));
    console.log(JSON.stringify(report, null, 2));
    process.exitCode = report.passed ? 0 : 1;
  } catch (error) {
    console.error(error.message);
    process.exitCode = 1;
  }
}
