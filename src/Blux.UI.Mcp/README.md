# Blux.UI.Mcp

A local [Model Context Protocol](https://modelcontextprotocol.io) server for [Blux UI](https://www.nuget.org/packages/Blux.UI),
for AI coding agents. It answers over stdio, offline, from the documentation of the Blux UI version it was built with:
component search, parameters, events and slots, theme slots, example sources, guides and installation steps.

```bash
dotnet tool install -g Blux.UI.Mcp --prerelease
claude mcp add --transport stdio blux-ui -- blux-ui-mcp
```

Tools: `search-components`, `get-component`, `get-component-metadata`, `list-examples`, `get-example`,
`search-documentation`, `get-documentation-page`, `search-icons`, `get-installation-guide`. Prompts:
`find-component-for-usecase`, `implement-component-with-props`. Resources: `resource://blux-ui/components`,
`resource://blux-ui/examples`, `resource://blux-ui/documentation-pages`.

Options: `--tools a,b` exposes only those tools, `--site-url https://...` prefixes page paths in answers, `--list-tools`, `--version`.
