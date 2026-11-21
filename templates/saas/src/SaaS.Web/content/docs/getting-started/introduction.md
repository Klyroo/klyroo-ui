---
title: Introduction
description: What Tideline watches, and how a ping turns into an alert.
order: 1
---

Tideline watches the jobs that run on a schedule: backups, exports, reports, queue sweeps, certificate renewals. These jobs have no user waiting for them, so when one stops, nobody notices until the damage shows up somewhere else.

## How it works

Every job gets a **monitor**, and every monitor has a URL. The job calls that URL when it finishes. Tideline knows the schedule, so it knows when the next ping is due, and if it does not arrive in time it alerts the people you chose.

```text
your job --ping--> Tideline --alert--> email, Slack, PagerDuty, webhook
                      ^
                expects a ping
                every 24 hours
```

Nothing in your network has to accept a connection from Tideline. The job calls out, and that is the only direction anything travels.

## What you need

- A job that can make an HTTP request: `curl`, `wget`, a line of Python or any HTTP client does it.
- An account. The free plan has five monitors and does not ask for a card.

::: tip Start small
Monitor the one job whose failure you would hate to find out about from a customer. Add the others once you have seen an alert arrive.
:::

## Where to go next

1. Follow the [quick start](quick-start.md) to create a monitor and send a first ping.
2. Read [concepts](concepts.md) for the words the rest of the documentation uses.
3. Pick a [guide](../guides/monitor-a-cron-job.md) for your kind of job.
