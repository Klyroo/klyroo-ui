---
title: Configuration
description: Where Tern reads its settings from and what each setting does.
order: 1
---

Tern reads its settings from a configuration file, from environment variables and from command line flags. When the same setting comes from more than one place, the more specific place wins.

## Where settings come from

From the least to the most specific:

1. The user file, `config.yml` in the folder that `tern --version` prints.
2. The folder file, `tern.yml` in the folder you synchronize.
3. Environment variables that start with `TERN_`, listed in the [reference](/reference/environment-variables).
4. Flags on the command, such as `--retries 5`.

A setting that is not set anywhere uses its default.

## The folder file

`tern init` writes `tern.yml` for you. Keep it in version control with the folder, so that everyone who clones the folder synchronizes it with the same remote.

```yaml
remote: s3://my-bucket/photos
region: eu-west-1
retries: 3
timeout: 30s
parallel: 4
ignore:
  - "*.tmp"
  - node_modules/
```

The same settings in JSON, if you prefer it, in a file named `tern.json`:

```json
{
  "remote": "s3://my-bucket/photos",
  "region": "eu-west-1",
  "retries": 3,
  "timeout": "30s",
  "parallel": 4,
  "ignore": ["*.tmp", "node_modules/"]
}
```

Only one of `tern.yml` and `tern.json` may exist in a folder. Tern stops with an error when it finds both.

## Settings

| Setting | Default | What it does |
| --- | --- | --- |
| `remote` | none | The bucket and the prefix, as `s3://bucket/prefix`. Required. |
| `region` | `us-east-1` | The region of the bucket. |
| `retries` | `3` | How many times a failed transfer is tried again. |
| `timeout` | `30s` | How long one transfer may take before it counts as failed. |
| `parallel` | `4` | How many files are transferred at the same time. |
| `ignore` | empty | Patterns of files to leave out. See [ignore rules](ignore-rules.md). |

> [!IMPORTANT]
> Never put the key and the secret in `tern.yml`. The file is meant to be committed. Use the environment variables `TERN_KEY_ID` and `TERN_KEY_SECRET`, or the user file.

## The user file

The user file holds what is the same for every folder: the region, the number of parallel transfers and, if you want, the credentials. Its settings are the same as the folder file's, except `remote` and `ignore`.

```yaml
region: eu-west-1
parallel: 8
```

## Check what Tern uses

`tern config` prints the settings that apply to the current folder and the place each one came from.

```bash
tern config
```
