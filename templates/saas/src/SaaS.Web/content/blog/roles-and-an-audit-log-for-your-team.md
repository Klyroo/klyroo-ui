---
title: Roles and an audit log for your team
description: Viewers, members and admins, and a list of who changed what, for the teams that asked for both.
date: 2026-09-09
author: Noor Haddad
authorRole: Co-founder
tags: [product, teams]
image: img/blog/lanterns.svg
imageAlt: A row of round lanterns in teal and yellow.
---

Until this month, everyone on an account could do everything. That is fine for a team of three, and it stops being fine when a contractor needs to see the dashboard and should not be able to delete a monitor.

## Three roles

| Role | Can |
| --- | --- |
| Viewer | See monitors, incidents and history. |
| Member | Create, edit and pause monitors. |
| Admin | Manage channels, people and billing. |

Everyone who was on an account before this change is an admin, so nothing stops working today. You can lower anyone's role from **Settings**, then **People**.

## An audit log

Each change now leaves a line: who did it, what they did and when. Changing a schedule, pausing a monitor, adding a channel and inviting a person are all there. The log is kept as long as your history, and you can filter it by person.

We built it after an incident on a customer's account where a schedule had been edited and nobody could say by whom. The next time, the answer is a click away.

## Availability

Roles and the audit log are part of the Scale plan. The [team access guide](/docs/guides/team-access) describes how to set them up.
