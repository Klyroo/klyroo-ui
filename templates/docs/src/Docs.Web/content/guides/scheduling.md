---
title: Scheduling
description: Run Tern every night with cron, a systemd timer or a scheduled task.
order: 3
---

Tern does not run in the background. To synchronize a folder on a schedule, start `tern sync` from the scheduler of your system and let its exit code tell the scheduler how it went. The codes are listed in the [reference](/reference/exit-codes).

## Before you schedule it

Run the command by hand first, from the same account that the scheduler will use, with the same environment variables. Most scheduled jobs that fail on their first night fail because the key is set in your shell and not in the job.

## cron

Edit the table with `crontab -e` and add a line. This one runs every night at 02:30 and appends the output to a log file.

```bash
30 2 * * * cd /home/ana/photos && /usr/local/bin/tern sync --quiet >> /home/ana/tern.log 2>&1
```

## systemd timer

A timer needs two files in `~/.config/systemd/user`. The first describes the job.

```text
[Unit]
Description=Synchronize the photos folder

[Service]
Type=oneshot
WorkingDirectory=%h/photos
EnvironmentFile=%h/.config/tern/env
ExecStart=/usr/local/bin/tern sync --quiet
```

The second says when to run it.

```text
[Unit]
Description=Synchronize the photos folder every night

[Timer]
OnCalendar=*-*-* 02:30:00
Persistent=true

[Install]
WantedBy=timers.target
```

Enable the timer and check that it is waiting:

```bash
systemctl --user enable --now tern-photos.timer
systemctl --user list-timers
```

> [!TIP]
> `Persistent=true` makes systemd run a job that was missed because the machine was off, as soon as the machine is on again.

## Windows scheduled task

Create a task that runs `tern.exe sync --quiet` in the folder, at the time you want, under your own account. Set the two environment variables for the user, not for the session.

## Make the output easy to read later

`--quiet` prints only warnings and errors. `--json` prints one JSON object per file, which is easier to read from a script than the default text.

```bash
tern sync --json | jq 'select(.action == "upload")'
```
