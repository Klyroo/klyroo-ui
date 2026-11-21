---
title: Exit codes
description: What the number returned by tern means, for scripts and schedulers.
order: 3
---

Every command ends with an exit code. `0` means success, and any other number means the command did not finish what it was asked to do.

| Code | Meaning |
| --- | --- |
| `0` | The command finished. For `status`, there are no differences. |
| `1` | For `status`, there are differences. For other commands, an unspecified error. |
| `2` | The command line is wrong: an unknown command or flag, or a missing argument. |
| `3` | The configuration cannot be used: no `remote`, a file that does not parse, or both `tern.yml` and `tern.json`. |
| `4` | Access was refused by the storage provider. |
| `5` | Some files could not be transferred after all retries. The others were transferred. |
| `6` | The network or the storage provider could not be reached. |

## Use them in a script

```bash
tern sync --quiet
case $? in
  0) echo "synchronized" ;;
  5) echo "some files failed, try again later" ;;
  *) echo "tern failed" >&2; exit 1 ;;
esac
```

## Check for differences

`tern status` returns `1` when the two sides differ, so a script can branch on it without reading the output.

```bash
if tern status --quiet; then
  echo "in step"
else
  echo "needs a sync"
fi
```

> [!NOTE]
> Code `5` is not a failure of the whole run. Files that were transferred stay transferred, and running the command again only retries the ones that failed.
