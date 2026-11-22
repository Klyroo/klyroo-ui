---
title: "Tideline 2: status pages, a new command line and a quieter dashboard"
description: The biggest release since we started, with a public status page, the tideline command and a dashboard that shows less.
date: 2026-11-12
author: Noor Haddad
authorRole: Co-founder
tags: [product, announcements]
image: img/blog/waves.svg
imageAlt: Layered blue and teal waves.
---

Tideline 2 is out today. It is the first release that was shaped mostly by what you asked for in the last year, and it has three parts.

## A status page for your customers

Every account can now publish a status page at an address you choose. It lists the monitors you pick, shows their last 90 days as a strip of green and red days, and opens an incident for the people who subscribe when one of those monitors goes down.

The page is static HTML behind a CDN, so it stays up when we have a bad day. That was the one requirement we would not trade away.

## A command line that wraps your job

The new `tideline` command runs a job and takes care of the pings:

```bash
tideline run nightly-backup -- /opt/backup/run.sh
```

It sends the start signal, runs the command, and sends the success or the failure with the exit code. The exit code of your command is the exit code of `tideline run`, so your scheduler still sees what happened. The [command line reference](/docs/reference/command-line) has the rest.

## A dashboard that shows less

The old dashboard showed every monitor as a card with seven numbers. The new one shows a list, and a number only when it is worth your attention: a duration that grew, a ping that was late three times this week.

::: tip Upgrading
Nothing changes for your jobs: the ping URLs are the same. The new dashboard is on by default, and the old one stays available in **Settings** until the end of the year.
:::

Thank you to everyone who wrote to us. The full list of changes is in the [changelog](/changelog/2-0).
