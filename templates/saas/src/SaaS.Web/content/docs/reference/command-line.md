---
title: Command line
description: Wrap a job with the tideline command, and manage monitors from a terminal.
order: 2
---

The `tideline` command sends the pings for you, and manages monitors without opening the dashboard.

## Install

```bash
curl -fsSL https://example.com/install.sh | sh
tideline --version
```

## Sign in

```bash
tideline login
```

The command opens a page of the dashboard and stores a token in `~/.config/tideline/token`, readable by you only. In a CI job, set `TIDELINE_TOKEN` instead.

## Run a job with a monitor

`tideline run` starts the command, sends the start signal, then the success or the fail signal with the exit code, and returns the exit code of the command.

```bash
tideline run nightly-backup -- /opt/backup/run.sh --full
```

Use it in a crontab in place of the `curl` lines:

```bash
0 2 * * * tideline run nightly-backup -- /opt/backup/run.sh
```

## Manage monitors

| Command | What it does |
| --- | --- |
| `tideline monitors list` | Lists the monitors with their state. |
| `tideline monitors create NAME --schedule "0 2 * * *" --grace 15m` | Creates a monitor and prints its ping URL. |
| `tideline monitors pause NAME` | Pauses a monitor, for a maintenance. |
| `tideline monitors resume NAME` | Resumes it. |
| `tideline monitors delete NAME` | Deletes it, and its history. |

## Output and exit codes

Add `--json` to a command to print JSON instead of a table, so that a script can read it.

| Code | Meaning |
| --- | --- |
| 0 | The command succeeded. |
| 1 | The command failed: read the message on the error output. |
| 2 | The arguments are not valid. |
| 3 | Not signed in, or the token was refused. |

`tideline run` returns the exit code of the wrapped command instead, so a failure of the job stays a failure for the scheduler.
