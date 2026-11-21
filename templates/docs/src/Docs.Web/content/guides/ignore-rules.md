---
title: Ignore rules
description: Keep build output, caches and secrets out of the bucket with a .ternignore file.
order: 2
---

Tern leaves out every file that matches an ignore rule, on both sides. An ignored file is never uploaded, never downloaded and never deleted, so a rule is also a way to protect something in the bucket that the folder does not have.

## Where rules live

Rules come from two places, and both apply:

- the `ignore` list in `tern.yml`, and
- a file named `.ternignore` in the folder, or in any folder below it, with one rule per line.

## Syntax

The syntax is the one of a `.gitignore` file.

```text
# A comment
*.tmp
.cache/
/build
notes/**/*.draft
!keep.tmp
```

Each line means:

- `*.tmp` matches a file with that extension in any folder.
- `.cache/` matches a folder with that name and everything in it.
- `/build` matches only `build` at the top of the folder.
- `**` matches any number of folders.
- A line that starts with `!` brings back a file that an earlier rule left out.
- A line that starts with `#` is a comment.

## Test a rule

`tern status --ignored` lists the files that the rules leave out, so you can see the effect of a change before you synchronize.

```bash
tern status --ignored
```

::: caution Rules do not remove files
Adding a rule for a file that is already in the bucket does not delete it. Tern stops looking at the file, and the copy in the bucket stays where it is. Delete it with your storage provider's tools if you want it gone.
:::

## Common rules

```text
# Build output
bin/
obj/
dist/

# Dependencies
node_modules/

# Editor files and operating system files
.vscode/
.idea/
.DS_Store
Thumbs.db

# Secrets
.env
*.pem
```
