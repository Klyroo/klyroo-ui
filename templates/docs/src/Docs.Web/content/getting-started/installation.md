---
title: Installation
description: Install the tern executable on Linux, macOS or Windows.
order: 2
---

Tern is a single executable. Install it with a package manager, or download the archive for your system and put the executable on your path.

## Requirements

- A 64-bit Linux, macOS 13 or later, or Windows 10 or later.
- An account with a storage provider that speaks the S3 protocol, and a key that can read and write one bucket.
- About 30 MB of disk space.

## Install with a package manager

On macOS and Linux with Homebrew:

```bash
brew install tern
```

On Windows with winget:

```text
winget install Tern.Tern
```

## Download the archive

Each release has one archive per system. Download it, check the checksum that is published next to it, and unpack it into a folder on your path.

```bash
curl -LO https://example.com/tern/releases/tern-1.4.0-linux-x64.tar.gz
sha256sum tern-1.4.0-linux-x64.tar.gz
tar -xzf tern-1.4.0-linux-x64.tar.gz
sudo mv tern /usr/local/bin/
```

::: warning Check the checksum
Compare the output of `sha256sum` with the value on the release page before you unpack the archive. An archive that does not match has been damaged or replaced.
:::

## Check the installation

Ask Tern for its version. The first line is the version, and the second is the folder where it looks for its configuration.

```bash
tern --version
```

```text
tern 1.4.0
config: /home/ana/.config/tern
```

If the shell answers `command not found`, the folder that holds the executable is not on your path. Add it, open a new terminal and try again.

## Update and uninstall

`brew upgrade tern` and `winget upgrade Tern.Tern` update a package manager install. For a downloaded archive, repeat the download and replace the executable. To remove Tern, delete the executable and the `config` folder shown by `tern --version`.
