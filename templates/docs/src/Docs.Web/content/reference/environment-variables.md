---
title: Environment variables
description: The variables Tern reads, and which setting each one replaces.
order: 2
navigationTitle: Environment
---

Every variable starts with `TERN_`. A variable replaces the same setting in the configuration files, and a flag on the command replaces the variable. The [configuration guide](/guides/configuration) explains the order.

| Variable | Setting | Notes |
| --- | --- | --- |
| `TERN_KEY_ID` | credentials | The access key id. |
| `TERN_KEY_SECRET` | credentials | The secret that goes with the key id. |
| `TERN_REMOTE` | `remote` | For example `s3://my-bucket/photos`. |
| `TERN_REGION` | `region` | For example `eu-west-1`. |
| `TERN_RETRIES` | `retries` | A whole number. |
| `TERN_TIMEOUT` | `timeout` | A number and a unit: `30s`, `2m`. |
| `TERN_PARALLEL` | `parallel` | A whole number from 1 to 32. |
| `TERN_CONFIG_DIR` | none | The folder of the user file. |
| `NO_COLOR` | none | When set to any value, Tern prints without color. |

## Set them for one command

On Linux and macOS, put the assignment before the command:

```bash
TERN_REGION=eu-west-1 tern sync --dry-run
```

On Windows, set the variable for the user so that scheduled tasks see it too:

```text
setx TERN_KEY_ID "AKIAEXAMPLE"
```

> [!WARNING]
> A variable that you set on the command line stays in your shell history. Put the key and the secret in the user file or in the environment of the scheduler instead.
