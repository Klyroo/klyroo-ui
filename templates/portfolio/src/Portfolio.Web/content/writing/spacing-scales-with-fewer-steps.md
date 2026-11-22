---
title: Spacing scales with fewer steps
description: Most design systems ship a spacing scale with twelve steps and use four of them. What I do instead, and what it saves.
date: 2026-05-20
tags: [design-systems, css]
image: img/writing/spacing-scale.svg
imageAlt: Six white bars of rising height on a green background.
---

When I count the spacing values used by a shipped product, the number is always larger than the scale it claims to follow, and the values that matter are few. Three or four of them account for nearly all of the space in a product, and the rest are exceptions that became habits.

## What the numbers say

On the audit of one product, the scale had 14 steps. Of the 1,900 places that set a margin or a gap, 82 percent used one of five values, and 11 steps appeared fewer than 20 times each.

A long scale looks flexible and is hard to use. Each choice is a small decision, and small decisions made by different people drift: 12 pixels here, 14 there, because nothing says which is right.

## The scale I start with

Eight steps, each roughly 1.5 times the previous one, starting from a 4 pixel unit:

```css
:root {
  --space-1: 4px;
  --space-2: 8px;
  --space-3: 12px;
  --space-4: 16px;
  --space-5: 24px;
  --space-6: 32px;
  --space-7: 48px;
  --space-8: 72px;
}
```

The steps are far enough apart that nobody has to wonder whether the difference is visible, and that is the point. A scale is a way to stop people from choosing between 12 and 14.

## Rules that make it work

1. **Space inside is smaller than space between.** Padding inside a card uses a lower step than the gap between two cards, so the grouping shows without borders.
2. **One step per relationship.** Related things sit at one step, the unit and its parts at the next lower one. If you need two values for the same relationship, the layout has a problem.
3. **Components own their padding, layouts own the gaps.** A component never sets its own outside margin.

::: tip
If a design needs a value that is not on the scale, ask whether the design is wrong before you add a step. It is wrong about as often as the scale is.
:::

## What it saved

On the project that adopted this, the number of distinct spacing values fell from 31 to 8, and a review of a screen no longer included a debate about 2 pixels. The designers did not feel constrained. They said it made their work faster, and I believe them, because there was less to decide.
