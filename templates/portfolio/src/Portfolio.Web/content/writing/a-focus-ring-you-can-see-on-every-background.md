---
title: A focus ring you can see on every background
description: A single outline color fails on half your surfaces. Here is a two-layer ring that stays visible on light, dark and photographs, in four lines of CSS.
date: 2026-10-02
tags: [accessibility, css]
image: img/writing/focus-ring.svg
imageAlt: A rounded rectangle surrounded by alternating yellow and black rings.
---

Most focus styles are one color. They pass the contrast check on the page background and then vanish on a dark card, a colored button or a photograph. Every team I have worked with had at least one of these bugs, and they find out from a keyboard user, not from a test.

## Why one color is not enough

The WCAG success criterion for non-text contrast asks for 3:1 between the focus indicator and what is next to it. "What is next to it" changes with every surface the component can sit on. A blue ring that works on white is nearly invisible on a navy header.

You can pick a color per surface, and teams do, until the day someone puts a component on a surface nobody planned for.

## Two layers

The fix is old and works everywhere: draw the ring in two colors, one light and one dark, so that at least one of them contrasts with whatever is behind.

```css
:focus-visible {
  outline: 2px solid var(--focus-dark);
  outline-offset: 2px;
  box-shadow: 0 0 0 2px var(--focus-light);
}
```

The outline is the dark layer, and the box shadow, which is drawn inside the outline offset, is the light one. On a white page you see the dark ring with a faint white halo. On a navy header you see the light ring against the dark, and the dark one beside it.

## What to check

- **Both layers are 2 pixels or more.** A thinner ring disappears when a screen is scaled down.
- **The offset is visible.** An offset of 2 pixels leaves a gap in which the light layer shows. With no offset the layers merge and you get one thick ring of the wrong color.
- **Rounded corners follow the component.** Use `outline` with `border-radius` on the element, since browsers now curve the outline to match it.
- **Forced colors mode.** Windows High Contrast ignores `box-shadow`, but it keeps `outline`, so keep the outline as the part that always works.

::: tip
Test it by tabbing through a page with the background of every component set, one after another, to white, black and a busy photograph. It takes ten minutes and finds more than any automated check.
:::

## The cost

The two-layer ring is a little heavier than a single line, and some designers dislike that at first. I have not yet met a team that, after the first keyboard bug report they did not have to read, asked to go back.
