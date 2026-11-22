---
title: Grace periods without the guesswork
description: How we worked out a default for the grace period from a year of ping data, and why it is shorter than you would think.
date: 2026-09-30
author: Camila Duarte
authorRole: Product designer
tags: [product, monitoring]
image: img/blog/dunes.svg
imageAlt: Smooth dunes in pale orange and purple.
---

When you create a monitor, the form asks for a grace period, and until now it suggested ten minutes whatever you typed in the schedule field. That was a guess, and a bad one. We looked at what jobs actually do.

## What the data says

We took the pings of the monitors that have run for more than a month, and looked at how much a job's duration varies from one run to the next. The finding that surprised us was how regular most jobs are:

| Share of jobs | Longest run vs the median |
| --- | --- |
| 50% | up to 20% longer |
| 90% | up to 80% longer |
| 99% | up to 3 times longer |

A grace period of 150 percent of the longest run you have seen covers 90 percent of jobs and keeps an alert from firing on a normal slow night.

## What changed in the form

The new form has no fixed default. When you create a monitor from the first ping, it proposes a grace period from the runs it has seen. Before the first ping, it asks how long the job usually takes and proposes a value from the table above.

## What did not change

You can still set any value, and a monitor with a start signal can have a tight one, because Tideline then sees when the run began. Read the [guide on grace periods](/docs/guides/grace-periods) for how to choose by hand.
