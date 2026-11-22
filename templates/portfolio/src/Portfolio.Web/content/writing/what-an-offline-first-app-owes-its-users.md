---
title: What an offline-first app owes its users when it syncs
description: Offline-first is a promise about data, and the promise is kept or broken in the moment two copies meet. Four rules I use for the interface of a sync.
date: 2026-08-14
tags: [engineering, local-first]
image: img/writing/offline-sync.svg
imageAlt: Two white cards joined by a dotted line and a yellow curve.
---

When people talk about offline-first apps they talk about storage and conflict resolution. Those are the hard engineering problems, and they are not the ones users notice. A user notices whether the app told them the truth about their notes.

I worked on [Fieldnote](/work/fieldnote), a notebook for researchers who work without signal, and these are the rules the interface ended up with.

## 1. Say where the data is

"Saved" is ambiguous in an app that has two places to save. The user wants to know one thing: if I lose this phone now, is it gone? We wrote the answer on each note: "Saved on this device" or "Saved everywhere". It is two states, with words and an icon, and it never changes color to suggest an emergency.

## 2. Never block on the network

Every action works against local data and finishes at once. The network is a background job with its own small status line, and it can fail without the interface noticing. If a button has to disable itself while a request runs, the app is online-only with a cache.

## 3. Make conflicts visible and rare

Most conflicts can be avoided by the shape of the data. Notes are mostly appended, so merging is routine. For the rest, we kept both versions as revisions, with the later one shown first and the other one a tap away.

| Kind of change | What happens |
| --- | --- |
| Two people add text to a note | Merged as text, nobody is asked |
| Two people edit the same measurement | Both kept, later one shown, a small mark says so |
| One person deletes, another edits | The edit wins, and the note comes back |

The last row surprises engineers, and it is the right default: bringing something back is cheap, and losing someone's work is not.

## 4. Do not ask the user to do the sync

An app that makes users press "sync" has given them a job. Sync when the connection returns, and when the app is opened, and when it is closed. Provide a button for the person who wants it, and make sure it is never required.

::: note
A small amount of data goes a long way. The whole state of a researcher's season, in the format we chose, was under 4 MB, which let us sync on the worst connections the lab had.
:::

## What it adds up to

The promise is simple to state: what you wrote is safe, and the app will tell you the truth about where it is. Everything else, including the CRDTs, is how you keep it.
