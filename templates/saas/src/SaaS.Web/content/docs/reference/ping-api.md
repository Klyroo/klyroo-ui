---
title: Ping API
description: The URLs a job calls to report that it started, finished or failed.
order: 1
---

The ping API is a handful of URLs below `https://ping.example.com/p/`. It needs no key: the slug of the monitor in the address is the credential, so keep it out of public logs and repositories.

## Endpoints

| Request | Meaning |
| --- | --- |
| `GET /p/{slug}` | The job finished successfully. |
| `GET /p/{slug}/start` | The job started. |
| `GET /p/{slug}/fail` | The job failed. |
| `GET /p/{slug}/{code}` | The job ended with this exit code: 0 is a success, any other number a failure. |
| `POST /p/{slug}` | Like `GET`, and the body (up to 10 KB of text) is stored with the ping. |

`HEAD` works like `GET`. The address is the same over HTTP and HTTPS, and the clients that cannot do HTTPS can use the former; prefer the latter.

## Signals

A **start** signal opens a run, and the next success, fail or exit code closes it. The time between them is the duration of the run. A **fail** signal opens an incident at once, without waiting for the grace period.

```bash
# Send the output of the job with the ping, to see it in the history.
/usr/local/bin/export-orders 2>&1 | curl -fsS -m 10 --retry 3 --data-binary @- https://ping.example.com/p/orders-export
```

## Responses

| Status | Meaning |
| --- | --- |
| `200` | The ping was recorded. The body is `OK`. |
| `404` | No monitor has this slug, or it was deleted. |
| `413` | The body is larger than 10 KB. |
| `429` | More than 5 pings a second for this monitor. Slow down. |

A paused monitor answers `200` and ignores the ping.

## Retries

Send a ping again when the request fails or times out: a duplicate success is harmless, and a missing one is an alert. `--retry 3` of `curl` is enough for a short network problem.

## Read the state of a monitor

The monitor has a badge address that answers with its state as an image, for a README or a wiki page:

```text
https://ping.example.com/b/{slug}.svg
```
