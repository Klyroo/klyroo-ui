---
title: The audit I run before writing a single token
description: A design system starts with an inventory of what already ships, and a script is the fastest way to take it. The method, and what to do with the numbers.
date: 2026-02-11
tags: [design-systems, process]
image: img/writing/audit.svg
imageAlt: Five white rows with a check mark on each, one marked in red.
---

The first meeting about a design system usually has a designer with a mood board and an engineer with a list of components. Both are useful, and neither tells you what the products look like today. That is what an audit is for, and it is the only part of the work that persuades the doubtful teams.

## Take the inventory by machine

Read the built CSS of every product and count. You do not need a tool beyond a script and a headless browser: load each page, take every computed color, font size and spacing value from the elements on it, and write them to a file.

```bash
node audit.mjs --urls urls.txt --out audit.json
jq '.colors | length' audit.json
```

I run it over the ten most visited pages of each product, in light and dark mode if there is one. The result is a set of counts, and counts are hard to argue with: 212 colors, 38 font sizes, 31 spacing values.

## Cluster before you decide

Many of the values are the same value written differently. Group colors that are close enough to be mistaken for each other, and sizes within a pixel or two. What is left is the real list, usually a fifth of the original.

## Take it to the teams

This is the step that decides whether the system gets used. For each product, sit with the team that owns it and go through its values:

- which ones are intended, and which are accidents?
- which ones can be replaced by the nearest one in the proposed set, today?
- which ones cannot, and why?

You will hear things you could not have learned from the code: a color that is a legal requirement, a size that was chosen after a user test, a screen that nobody dares to touch.

## What you end up with

A token set with the owners already inside it, and a list of components ordered by how many products have their own version. I only build a component once three products have one, which keeps the library small and keeps every component tied to a real need.

## What I do not do

I do not ship the audit as a report. A report is read once and filed. The numbers go into the first meeting, into the migration plan, and onto a screen in the team room where the count of distinct colors goes down as the work goes on.
