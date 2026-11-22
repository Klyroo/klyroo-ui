---
title: How we keep ping latency under 40 ms
description: A ping is a tiny request, and the way we handle it is the reason the monitoring does not slow your jobs down.
date: 2026-08-19
author: Elias Brandt
authorRole: Engineer
tags: [engineering]
image: img/blog/tide.svg
imageAlt: Horizontal bands of water in shades of blue.
---

A job that pings Tideline waits for the answer, however briefly. If that wait were long, the monitoring would make the job slower, and people would stop using it. So the budget for a ping is small: a median of under 40 milliseconds from the machine that sends it.

## Answer first, write after

The ping handler does three things: it checks that the slug exists, it replies `200`, and it records the ping. Only the first needs to happen before the reply. The record goes on a queue and is written a moment later, in batches.

```csharp
app.MapGet("/p/{slug}", async (string slug, PingQueue queue, MonitorIndex monitors) =>
{
    if (!monitors.Contains(slug))
    {
        return Results.NotFound();
    }

    // The queue is in memory: this does not wait for the database.
    queue.Enqueue(new Ping(slug, DateTimeOffset.UtcNow));
    return Results.Text("OK");
});
```

## The index lives in memory

The list of valid slugs is a small hash set held in memory by every node, and updated when a monitor is created or deleted. Checking a slug never touches the database.

::: note Durability
The queue is flushed every 200 milliseconds and on shutdown. A node that crashes can lose the last pings it had not yet written, which is why a job that retries a failed request is a better citizen than one that sends once.
:::

## Where the time goes

Of the 40 milliseconds, about 28 are the network between your machine and ours. The handler takes under one millisecond. That is why we run in several regions, and why the ping address is the same everywhere: your request reaches the nearest one.
