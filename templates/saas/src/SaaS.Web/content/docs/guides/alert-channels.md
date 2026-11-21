---
title: Alert channels
description: Send alerts by email, to Slack, to PagerDuty or to your own webhook.
order: 2
---

A channel is a place an alert goes. A monitor can use several, and a channel can serve several monitors.

## Channels

| Channel | Plans | Needs |
| --- | --- | --- |
| Email | All | An address; each person can confirm their own |
| Slack | Team and Scale | An incoming webhook URL of the channel |
| PagerDuty | Team and Scale | The integration key of a service |
| Webhook | Team and Scale | An HTTPS address that accepts a `POST` |

## Add a channel

1. Open **Settings**, then **Channels**.
2. Choose **Add channel**, pick its type and fill in what it needs.
3. Press **Send a test alert**: a channel that has not received one is not worth trusting yet.
4. Open a monitor and tick the channel under **Alerts**.

## Reminders

A monitor that stays down alerts once when it goes down, and again after the interval you set (every 30 minutes by default) until someone acknowledges the incident or the next ping arrives.

## The webhook payload

A webhook receives JSON like this when an incident opens or closes:

```json
{
  "event": "incident.opened",
  "monitor": { "name": "nightly-backup", "slug": "nightly-backup" },
  "state": "down",
  "reason": "no ping within the grace period",
  "expected_at": "2026-11-12T02:00:00Z",
  "opened_at": "2026-11-12T02:15:00Z",
  "url": "https://app.example.com/incidents/8421"
}
```

`event` is `incident.opened` or `incident.closed`. Tideline sends a `User-Agent` of `Tideline-Webhook/1` and waits 10 seconds for an answer in the 2xx range; a failed delivery is tried again three times, a minute apart.

::: warning Check who sent it
Anyone who finds the address of your webhook can send it a payload. Use a long random path, or check the `X-Tideline-Signature` header, which holds an HMAC-SHA256 of the body made with the secret of the channel.
:::
