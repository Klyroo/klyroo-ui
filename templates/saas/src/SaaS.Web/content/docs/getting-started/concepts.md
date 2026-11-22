---
title: Concepts
description: Monitors, pings, schedules, grace periods and incidents.
order: 3
---

The words below mean the same thing in the dashboard, in the API and in these pages.

## Monitor

A monitor stands for one job or one schedule. It has a name, a slug (the last part of its ping URL), a schedule, a grace period and a list of alert channels.

## Ping

A ping is an HTTP request to the URL of a monitor. Tideline accepts `GET`, `HEAD` and `POST`, and keeps the time, the duration (when the job also sent a start signal), the exit code and up to 10 KB of text from the body of a `POST`.

## Schedule

The schedule is when the next ping is due. Write it as a cron expression or as an interval:

| Kind | Example | Meaning |
| --- | --- | --- |
| Cron | `30 7 * * 1` | Mondays at 07:30, in the time zone of the monitor |
| Interval | `every 6 hours` | Six hours after the previous ping |

## Grace period

The grace period is how late a ping may be before it counts as missed. A backup that takes between 3 and 12 minutes needs a grace period of at least 10: shorter and every slow night is an alert. [Grace periods](../guides/grace-periods.md) explains how to choose it.

## States and incidents

| State | When |
| --- | --- |
| New | The monitor exists and has not received a ping yet. |
| Up | The last ping arrived on time. |
| Late | The ping is overdue but the grace period has not run out. |
| Down | The grace period ran out, or the job sent a fail signal. An incident opens and the alerts go out. |
| Paused | You paused it. Nothing is expected and nothing is sent. |

An incident closes when the next ping arrives. It records when it opened, when it closed and who was told.
