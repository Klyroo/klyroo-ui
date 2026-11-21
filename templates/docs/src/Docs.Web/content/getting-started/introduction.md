---
title: Introduction
description: What Tern is, what it does and what it leaves to you.
order: 1
---

Tern is a command line tool that keeps a local folder and a folder in remote storage in step. You point it at both, ask for a plan, read the plan, and let it copy what differs. It runs on Linux, macOS and Windows, and it is one executable with no service behind it.

This site describes a made up product. It exists to show what a documentation site built from Markdown looks like, and every command, flag and file name in it is invented.

## What Tern does

Tern compares the two sides by file size and checksum, then does one of three things for each file that differs: upload it, download it, or delete it. The direction depends on the command you run.

- `tern sync` makes the remote folder match the local one.
- `tern pull` makes the local folder match the remote one.
- `tern status` lists the differences and changes nothing.

Every command that changes files accepts `--dry-run`, which prints the plan and stops. Read the plan before you run a command for the first time on a folder that matters.

> [!NOTE]
> Tern does not merge changes. When a file differs on both sides, the direction of the command decides which side wins, and the other copy is replaced.

## What Tern leaves to you

Tern does not version files, encrypt them or schedule itself. If you need a history of earlier copies, turn on versioning in your storage provider. If you need to run Tern every night, the [scheduling guide](/guides/scheduling) shows how to do it with cron or a systemd timer.

## Where to go next

1. Install Tern with the [installation page](installation.md).
2. Follow the [quick start](quick-start.md) to synchronize your first folder.
3. Read the [configuration guide](/guides/configuration) before you use it for real.
