---
title: Harbor Atlas
description: A public map of ships, berths and wait times for the port authority of a mid-sized harbor city, built to load fast on the phones of dock workers.
year: 2025
role: Design and front-end
client: Veldmark Port Authority
stack: [TypeScript, MapLibre, D3, Playwright]
featured: true
image: img/work/harbor-atlas.svg
imageAlt: Contour lines of a coast in teal, with three marked points.
links:
  - label: Live site
    url: https://example.com/harbor-atlas
  - label: Source
    url: https://example.com/harbor-atlas/source
---

The port authority of Veldmark publishes where every ship is, which berth it will use and how long trucks wait at each gate. Before this project that information was a PDF, refreshed twice a day and read mostly on phones by drivers standing in a queue.

## The problem

Drivers wanted one answer: should I go to gate 2 or gate 4? The PDF made them read a table of forty rows, and the few who tried the old web map gave up while it loaded tiles of a city they already knew.

## What I did

I started by sitting at the gates for two mornings and watching what drivers did with their phones. Almost nobody zoomed or panned. They looked at the wait times, compared two numbers and left.

- **Answer first.** The page opens on the gates, with the wait time as the largest number and a bar that shows how it compares with the usual wait at that hour. The map is one tap away and not the default.
- **A map that does less.** The map shows ships and berths only, drawn from vector data with a handful of layers, and the page works without it. The first view is under 90 KB over the network.
- **Honest numbers.** A wait time older than ten minutes is shown greyed out with its age, rather than looking fresh. The feed from the gates is flaky, and drivers trust a number that admits it.

::: note
The city had a design system for its websites, but it had no pattern for live data that can be stale. The "stale" state we designed for this project has since been added to it.
:::

## Result

Median time to the first useful number fell from about 14 seconds on the old map to under 2 on a mid-range phone on a mobile connection. The authority stopped sending the PDF in March, and the gate supervisors now use the same page on the tablets at the booths.

The interface is tested with Playwright and axe on every change, including a run with the map script blocked, so the answer-first view cannot quietly depend on it.
