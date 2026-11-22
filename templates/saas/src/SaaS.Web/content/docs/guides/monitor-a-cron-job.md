---
title: Monitor a cron job
description: Ping from a crontab, a systemd timer or a CI schedule, and measure how long the job takes.
order: 1
---

Any scheduler can ping Tideline, because a ping is an HTTP request. This page shows the three common ones and how to measure the duration.

## Crontab

Chain the ping after the job with `&&`, so that it is sent only when the job exits with 0:

```bash
15 3 * * * /usr/local/bin/export-orders && curl -fsS -m 10 --retry 3 https://ping.example.com/p/orders-export
```

## Measure the duration

Send a start signal before the job and the ping after it. Tideline records the time between them, and shows a job that started and never ended as down:

```bash
#!/usr/bin/env bash
set -euo pipefail

PING=https://ping.example.com/p/orders-export

curl -fsS -m 10 --retry 3 "$PING/start" > /dev/null
/usr/local/bin/export-orders
curl -fsS -m 10 --retry 3 "$PING" > /dev/null
```

## Report a failure

A script that fails should say so, instead of leaving Tideline to notice the silence after the grace period:

```bash
if ! /usr/local/bin/export-orders; then
  curl -fsS -m 10 --retry 3 "$PING/fail" > /dev/null
  exit 1
fi
```

::: note Exit codes
`curl "$PING/$?"` sends the exit code of the last command: 0 is a success and anything else is a failure. See the [ping API](../reference/ping-api.md#signals).
:::

## systemd timer

Add the ping as a second command of the service that the timer starts:

```ini
[Service]
Type=oneshot
ExecStart=/usr/local/bin/export-orders
ExecStartPost=/usr/bin/curl -fsS -m 10 --retry 3 https://ping.example.com/p/orders-export
```

`ExecStartPost` runs only when `ExecStart` succeeded.

## A CI schedule

In a scheduled GitHub Actions workflow, add a last step. It runs only when the steps before it passed:

```yaml
- name: Ping Tideline
  run: curl -fsS -m 10 --retry 3 "$PING_URL"
  env:
    PING_URL: ${{ secrets.TIDELINE_PING_URL }}
```

Keep the URL in a secret: anyone who has it can send a success for that monitor.
