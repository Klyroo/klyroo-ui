---
title: Commands
description: Every tern command, with its arguments and flags.
order: 1
---

The general form is `tern <command> [arguments] [flags]`. Flags can come before or after the command. `tern --help` lists the commands, and `tern <command> --help` lists the flags of one.

| Command | Arguments | What it does | Changes files |
| --- | --- | --- | --- |
| `init` | `[folder] [remote]` | Links a folder to a remote and writes `tern.yml`. | `tern.yml` only |
| `sync` | none | Makes the remote match the local folder. | Remote |
| `pull` | none | Makes the local folder match the remote. | Local |
| `status` | none | Lists the differences between the two sides. | No |
| `config` | none | Prints the settings that apply and where each comes from. | No |
| `ignore` | `<pattern>` | Adds a pattern to the ignore list of `tern.yml`. | `tern.yml` only |

## Flags that every command accepts

| Flag | Meaning |
| --- | --- |
| `--verbose`, `-v` | Print every request. |
| `--quiet`, `-q` | Print only warnings and errors. |
| `--json` | Print one JSON object per line instead of text. |
| `--config <file>` | Read this file instead of `tern.yml`. |
| `--help`, `-h` | Show the help of the command. |

## init

```bash
tern init [folder] [remote]
```

Writes `tern.yml` in the folder, which is the current folder when you leave it out. Without `remote`, Tern asks for it. It refuses to overwrite an existing `tern.yml` unless you add `--force`.

## sync

```bash
tern sync [--dry-run] [--no-delete] [--retries <n>]
```

Uploads new and changed files, and deletes files from the remote that are not in the folder.

### Flags

- `--dry-run` prints the plan and changes nothing.
- `--no-delete` uploads but never deletes. The remote can then hold files that the folder no longer has.
- `--retries <n>` overrides the `retries` setting for this run.

## pull

```bash
tern pull [--dry-run] [--no-delete]
```

The same as `sync` in the other direction: downloads new and changed files, and deletes local files that are not in the remote.

## status

```bash
tern status [--ignored]
```

Prints one line per difference, and `in step` when there is none. With `--ignored` it also lists the files that the ignore rules leave out.

## config

```bash
tern config
```

Prints each setting with its value and its source: `default`, `user file`, `folder file`, `environment` or `flag`.

## ignore

```bash
tern ignore "*.log"
```

Appends the pattern to the `ignore` list in `tern.yml`, keeping the file's comments and order.
