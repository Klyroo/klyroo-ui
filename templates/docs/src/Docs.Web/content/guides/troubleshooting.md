---
title: Troubleshooting
description: The errors people meet most often, and what to check first.
order: 4
---

Run the failing command again with `--verbose`. It prints each request that Tern makes, which usually shows whether the problem is the key, the bucket name or the network.

## Access denied

The key is valid, but it is not allowed to do what the command asked.

- The key needs permission to list the bucket, read objects and, for `sync`, write and delete objects.
- The bucket name or the prefix in `remote` may be wrong. A key that is limited to `photos/` cannot list `photos-old/`.
- Check which key Tern uses with `tern config`. A key in the environment wins over a key in the user file.

## The plan wants to delete files I did not expect

`sync` makes the remote match the local folder, so a file that is in the bucket and not in the folder is a deletion. This happens when the folder is empty, is not the folder you meant, or has lost files.

::: warning Stop before you run it
If the plan lists far more deletions than you expect, do not run the command. Run `tern status` and look at the first lines. An empty or unmounted folder produces a plan that deletes everything.
:::

## A transfer times out

Large files on a slow connection can take longer than the `timeout` setting. Raise it, and lower `parallel` so that fewer files compete for the same connection.

```yaml
timeout: 120s
parallel: 2
```

## Words used in these messages

Tern error messages use a few terms with a precise meaning.

Remote
: The bucket and prefix that `remote` names, such as `s3://my-bucket/photos`.

Plan
: The list of uploads, downloads and deletions that a command is about to carry out, printed by `--dry-run`.

Checksum
: A short value computed from the contents of a file. Tern uses it to tell whether two files with the same size are the same file.

## Still stuck

Open an issue in the repository linked from the footer of this site. Include the output of `tern --version`, the command you ran with `--verbose`, and your `tern.yml` with the remote name removed if it is private.
