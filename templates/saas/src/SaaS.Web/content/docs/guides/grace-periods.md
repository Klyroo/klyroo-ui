---
title: Grace periods
description: Choose how late a ping may be before it counts as missed.
order: 3
---

The grace period is the time between the moment a ping was due and the moment Tideline raises an alert. It exists because jobs do not run in exactly the same time every day.

## Choose one

1. Look at the duration of the last 20 runs, in the history of the monitor.
2. Take the longest one and add 50 percent.
3. Never go below the shortest delay your team could act on: an alert at 02:01 for a job that finishes by 02:05 teaches people to ignore alerts.

| The job takes | Start with |
| --- | --- |
| Seconds | 2 minutes |
| 3 to 12 minutes | 15 minutes |
| About an hour | 1 hour |

## Start signals make it tighter

Without a start signal, the grace period has to cover the whole duration of the job, because Tideline only sees when it ends. With one, Tideline knows when the run began and can alert on a run that lasts longer than expected, so the grace period can stay short.

```bash
curl -fsS -m 10 --retry 3 https://ping.example.com/p/nightly-backup/start
```

## Time zones

A cron schedule is read in the time zone of the monitor, set when you create it. When a zone changes its clocks, a job that runs at 02:30 may not run on the night the hour is skipped. Tideline treats that as a missed ping, so give such a job a different time, or use an interval schedule.

::: tip Quiet by default
Raise the grace period of a monitor that has alerted for nothing twice. A false alarm costs more than a few extra minutes.
:::
