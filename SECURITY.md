# Security Policy

## Supported versions

Klyroo.UI is in preview. Security fixes land on `main` and ship in the next release of the latest minor line.

| Version | Supported |
| :--- | :--- |
| 0.1.x (preview) | Yes |
| < 0.1 | No |

Once 1.0 ships, the latest major version and the previous one receive security fixes.

## Reporting a vulnerability

**Do not open a public issue, discussion or pull request for a security problem.**

Report it privately through GitHub's private vulnerability reporting:
[github.com/klyroo/klyroo-ui/security/advisories/new](https://github.com/klyroo/klyroo-ui/security/advisories/new).

Include what you can of the following, it speeds up the fix:

- the affected package(s) and version(s), and the render mode or platform (Static SSR, Server, WebAssembly, MAUI);
- a description of the issue and its impact;
- the steps or a minimal project to reproduce it;
- any known mitigation.

## What happens next

1. A maintainer acknowledges the report and confirms whether it is accepted.
2. We work on a fix in a private fork and agree on a disclosure date with you.
3. We publish the fixed packages, then a GitHub Security Advisory that credits you, unless you prefer to stay anonymous.

## Scope

In scope: the code of this repository and the NuGet / npm packages built from it. Out of scope: vulnerabilities
of third-party dependencies that are already public (report them upstream; Dependabot tracks their updates here),
and the demo and documentation sites unless the issue comes from a Klyroo package.
