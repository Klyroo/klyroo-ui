---
title: Quire
description: The design system of a regional bank, from an audit of 14 apps to a token set and a component library that four teams share.
year: 2024
role: Lead design engineer
client: Meridian Credit
stack: [Design tokens, TypeScript, Web components, Storybook]
featured: true
image: img/work/quire.svg
imageAlt: A grid of rounded tiles in purple, orange and yellow.
links:
  - label: Case study
    url: https://example.com/quire/case-study
  - label: How I audit
    url: ../writing/the-audit-i-run-before-writing-a-token.md
---

Meridian Credit had fourteen web apps built over twelve years by seven teams. They all looked like the same bank from a distance and like different banks up close: nine shades of the brand blue, five date pickers and three ideas of what a disabled button is.

## The problem

Three earlier attempts at a shared library had stalled. Each started by designing components, and each lost to the same objection from the product teams: "it does not do what mine does". I was asked to try a fourth time and to make it stick.

## What I did

I did not start with components. I started with an audit, and I ran it as a conversation with each team rather than as a report about them.

1. **Inventory.** A script collected every color, font size and spacing value that shipped, from the built CSS of all fourteen apps. The numbers made the argument for me: 212 distinct colors, 38 font sizes.
2. **Tokens first.** I proposed a token set of 24 colors, 9 sizes and 8 spacing steps, then mapped each app's values to the nearest token with the team that owned the app. Most of the work was agreeing which of two blues was the real one.
3. **Components by demand.** We built a component only when three apps already had their own version of it. The first release had 11 components, chosen by the audit, and they were the ones that hurt most.
4. **Accessibility in the definition of done.** A component is not finished until its keyboard behavior, focus states and contrast are tested in CI.

```json
{
  "color": {
    "action": { "default": "#1f4fd8", "hover": "#1a42b3" },
    "text": { "default": "#14171f", "muted": "#4a5160" }
  },
  "space": { "1": "4px", "2": "8px", "3": "12px", "4": "16px" }
}
```

## Result

Four teams use Quire today and a fifth is migrating. The pull request that moved the largest app took two weeks, and removed about 9,000 lines of duplicated styles. An external accessibility audit a year later found 60 percent fewer issues in the migrated apps than in the ones that had not moved yet.

The part I am proudest of is not in the code: the team that maintains Quire now has a rule that the first answer to "can we add a variant?" is "show me the three places that need it".
