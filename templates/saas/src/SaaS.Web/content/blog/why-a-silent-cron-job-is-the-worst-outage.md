---
title: Why a silent cron job is the worst kind of outage
description: A job that fails loudly gets fixed in an hour. One that stops without a sound can go unnoticed for weeks.
date: 2026-10-14
author: Elias Brandt
authorRole: Engineer
tags: [engineering, monitoring]
image: img/blog/ridge.svg
imageAlt: Three overlapping hills in warm colors.
---

Most outages announce themselves. A page returns an error, a queue grows, a graph turns red, and someone is paged. A scheduled job that stops running does none of that.

## Nothing is there to fail

A web request fails while someone is waiting for it, and the failure is a response. A cron job has no one waiting. If the machine that ran it was replaced, if its user lost a permission, if the schedule was edited in the wrong line, the job does not fail. It does not run, and that leaves nothing in any log.

The usual cost shows up later and far away:

- backups that were never taken, found when someone needs a restore;
- invoices that were not sent, found when a customer asks;
- a certificate that was not renewed, found when the browser says so.

## Watch for the absence

You cannot alert on an error that is never written, so you alert on the missing success. The job reports in when it finishes, and a monitor that expects the report raises the alarm when it is late. That is all a heartbeat is.

Two details decide whether it is useful. The ping must come from the job itself, after the work, and only on success: a ping that is sent from a wrapper that ignores the exit code reports a lie. And the monitor must know the schedule, so that "late" means something. Read [how to choose a grace period](/docs/guides/grace-periods) before you set one.

## Start small

You do not need to monitor every job on day one. Pick the one whose failure you would find hardest to explain, and give it a monitor today. The [quick start](/docs/getting-started/quick-start) takes five minutes.
