---
title: Kiln
description: An online shop for a ceramics studio that sells one-off pieces, where every item can be bought exactly once.
year: 2022
role: Freelance design and development
client: Ostra Ceramics
stack: [C#, Blazor, SQLite, Stripe]
image: img/work/kiln.svg
imageAlt: Three ceramic vases in terracotta, mustard and dark teal on a warm background.
links:
  - label: Live site
    url: https://example.com/kiln
---

Ostra Ceramics makes about 30 pieces a week, and each one is different. A normal shop template assumes stock: a product with a quantity, a size selector and a "back in stock" email. None of that fits a bowl that exists once.

## The problem

The studio sold through social media, and the owner spent her evenings replying to "is this still available?" and keeping a spreadsheet of what had gone. She wanted a shop that looked like her work and that never sold the same bowl twice.

## What I did

I designed the shop around the idea that a piece is a unique object with a short life, and built it so that the code says so.

- **One piece, one page.** Each piece has its own page with photographs on a plain background, the exact dimensions and the glaze, and nothing else competing with it. A sold piece stays online, labelled as sold, so the page works as a record of the studio's work.
- **A reservation, not a cart.** Choosing a piece holds it for 15 minutes in the database. The hold is taken in a single transaction, so two visitors cannot both get it, and the second one sees what happened, in plain words.
- **A weekly drop.** New pieces are published on Thursday evenings. The page before the drop shows a countdown and the pieces as silhouettes, and works without a script.

```csharp
// A piece can only be held when nobody else holds it and it is unsold.
var held = await db.Pieces
    .Where(p => p.Id == id && p.SoldAt == null && (p.HeldUntil == null || p.HeldUntil < now))
    .ExecuteUpdateAsync(s => s.SetProperty(p => p.HeldUntil, now.AddMinutes(15)));

return held == 1 ? Hold.Taken : Hold.Unavailable;
```

## Result

The owner no longer answers availability questions, and the drops sell out in a few hours. The shop runs on a small server for the cost of a coffee a month, and the whole site is under 200 KB per page.
