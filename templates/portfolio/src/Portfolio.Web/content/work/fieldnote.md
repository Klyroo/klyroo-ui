---
title: Fieldnote
description: An offline-first notebook for ecologists who collect data in places with no signal, and sync it when they are back in range.
year: 2023
role: Freelance product design and front-end
client: Brightwater Ecology Lab
stack: [TypeScript, IndexedDB, Service workers, CRDTs]
featured: true
image: img/work/fieldnote.svg
imageAlt: A page of a notebook with ruled lines, a red margin and handwritten marks.
links:
  - label: Case study
    url: https://example.com/fieldnote/case-study
---

Researchers at Brightwater Ecology Lab spend weeks in wetlands and forests, writing down what they see. They used paper notebooks, because every app they had tried lost data the first time the signal dropped.

## The problem

Paper is hard to search and impossible to merge. At the end of a season, two people had often written about the same plot, and a student spent a week typing notes into a spreadsheet. The lab wanted an app that treated "no connection" as the normal case.

## What I did

The design had one rule: the app never asks the researcher to wait for the network, and never shows a state that means "I might have lost this".

- **Every write is local first.** Notes go to IndexedDB at once, and the interface only ever reads from there. The network is a background job.
- **Sync you can see.** A small line under each note says "Saved on this device" or "Saved everywhere", in words and with an icon that does not rely on color. Nothing in the app says "offline", because for these users it is not an error.
- **Merging that does not need a decision.** Notes are append-mostly, so two edits of the same note are merged as text with a CRDT, and the rare conflicting change to a measurement is kept as a visible revision instead of overwritten.
- **Big targets, low contrast fears.** The lab works outdoors in glare and often with gloves. Tap targets are at least 48 pixels, and the dark and light themes were both tested outside on a bright day.

::: tip
We tested in the field before we tested in the office. The first prototype failed on day one because the sun made the placeholder text unreadable. That is the kind of failure you do not find at a desk.
:::

## Result

In the first season 23 researchers made about 11,000 notes, and no one reported lost data. The week of spreadsheet typing at the end of the season disappeared, and the lab now exports straight to its analysis tools.
