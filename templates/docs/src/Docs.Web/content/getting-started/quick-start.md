---
title: Quick start
description: Synchronize your first folder in five steps.
order: 3
---

This page takes a folder of photos to a bucket and back. It assumes you have [installed Tern](installation.md) and have the name of a bucket and a key that can write to it.

Before you begin:

- [x] Tern is installed and `tern --version` answers.
- [x] You have a bucket name, such as `my-bucket`.
- [ ] The key is in the environment variables `TERN_KEY_ID` and `TERN_KEY_SECRET`.

## 1. Link the folder to the bucket

Run `init` in the folder, or give it the folder. It writes a file named `tern.yml` that records the remote.

```bash
tern init ./photos s3://my-bucket/photos
```

The file it writes looks like this:

```yaml
remote: s3://my-bucket/photos
retries: 3
ignore:
  - "*.tmp"
  - .cache/
```

## 2. Look at the plan

`--dry-run` lists what would change and changes nothing. Run it first on any folder you care about.

```bash
tern sync --dry-run
```

```text
upload   2023/beach.jpg        4.1 MB
upload   2023/harbour.jpg      3.8 MB
delete   2022/old-draft.jpg
2 uploads, 1 deletion, 0 downloads (dry run)
```

::: tip
The word `delete` in the plan means the file exists in the bucket and not in the folder. If that is not what you expect, stop and run `tern status` to see why the two sides differ.
:::

## 3. Synchronize

Run the same command without the flag. Tern prints one line per file as it goes, and a summary at the end.

```bash
tern sync
```

## 4. Change something and run it again

Add a photo, run `tern sync --dry-run`, and the plan lists one upload. Tern compares sizes and checksums, so files that did not change are not sent again.[^checksums]

## 5. Bring the files back to another machine

On the other machine, link an empty folder to the same bucket and pull.

```bash
tern init ./photos s3://my-bucket/photos
tern pull
```

## Next steps

Read the [configuration guide](/guides/configuration) to learn what else `tern.yml` can hold, and the [ignore rules](/guides/ignore-rules) to keep files out of the bucket.

[^checksums]: The first run on a large folder reads every file to compute its checksum, so it takes longer. Later runs reuse the checksums that Tern stored in the `.tern` folder.
