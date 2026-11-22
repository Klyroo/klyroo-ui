---
title: Limits
description: What each plan includes, and the limits that apply to every plan.
order: 3
---

## Per plan

| | Hobby | Team | Scale |
| --- | --- | --- | --- |
| Monitors | 5 | 50 | Unlimited |
| Users | 1 | 10 | 50 |
| Shortest schedule | 5 minutes | 1 minute | 1 minute |
| History | 7 days | 90 days | 1 year |
| Alert channels | Email | Email, Slack, PagerDuty, webhook | Email, Slack, PagerDuty, webhook |

The pricing page of the site has the prices.

## Every plan

| Limit | Value |
| --- | --- |
| Size of a ping body | 10 KB |
| Pings per second, for one monitor | 5 |
| Length of a monitor name | 80 characters |
| Length of a grace period | 1 minute to 7 days |
| Webhook answer time | 10 seconds |

## When you reach a limit

Monitors above the limit of your plan are paused, oldest last, and you get an email before it happens. Pings sent to a paused monitor answer `200` and are ignored.
