---
title: Quick start
description: Create a monitor and send a first ping in about five minutes.
order: 2
---

This page takes a job from nothing to a monitor that has received its first ping.

## Create a monitor

1. Create an account with **Get started** in the header, and open **Monitors**.
2. Choose **New monitor** and give it a name such as `nightly-backup`.
3. Set the schedule to **Every day at 02:00** and the grace period to **15 minutes**.
4. Pick the alert channel: your email is already there.

The monitor now shows a ping URL like this one:

```text
https://ping.example.com/p/nightly-backup
```

## Send a ping

Call the URL when the job ends. Run it from a terminal first, to see the monitor turn green:

```bash
curl -fsS -m 10 --retry 3 https://ping.example.com/p/nightly-backup
```

The flags are worth keeping: `-f` makes `curl` fail on an HTTP error, `-s` hides the progress bar and `-S` still prints errors, `-m 10` stops after ten seconds and `--retry 3` tries again if the network blinks.

## Add it to the job

In a crontab, chain the ping after the command with `&&`, so that a job that fails does not report success:

```bash
# every night at 02:00
0 2 * * * /opt/backup/run.sh && curl -fsS -m 10 --retry 3 https://ping.example.com/p/nightly-backup
```

::: warning A ping means "it worked"
Pinging after a command that failed hides the failure. Use `&&`, or report the failure with the [fail signal](../reference/ping-api.md#signals).
:::

## Check that an alert arrives

Stop sending pings, wait for the schedule and the grace period to pass, and an email arrives. To see it sooner, create a second monitor with a schedule of every five minutes and never ping it.
