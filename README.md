# Geisofanam

Unity project consuming **Funder Core** via the Package Manager.

## Documentation quick start (agents and contributors)

For low-token, high-signal project context, start here:

1. `Assets/Docs/START_HERE.md` (minimal context loading policy)
2. `Assets/Docs/FeatureRegistry.json` (active feature status/ownership index)
3. Target feature doc in `Assets/Docs/Features/`
4. Only the system docs required from `Assets/Docs/Systems/`

## Core package

`com.funder.core` is referenced from the package manager (see `Packages/manifest.json`), not embedded:

- **Git:** `https://github.com/fundergames/funder-core.git?path=Packages/com.funder.core#main`

Unity resolves this via the `path` query so the package is loaded from `Packages/com.funder.core` in the funder-core repo. Project code still loads core resources (e.g. `Resources.Load("FunderCore/FGAppConfig")`); ensure those assets exist in your project or are provided by the core package/samples.

## Unity MCP server setup (for AI tooling)

This project now includes `com.coplaydev.unity-mcp`, which enables Model Context Protocol (MCP) tools for Unity Editor automation.

### Enable in Unity

1. Open Unity and go to **Window > MCP for Unity**.
2. Click **Start Server** (default endpoint: `http://localhost:8080/mcp`).
3. Verify the status shows connected/running in the MCP for Unity window.

### Cursor MCP config

Use the repo template and copy it into your local Cursor config:

```bash
mkdir -p .cursor
cp cursor.mcp.json.example .cursor/mcp.json
```

The template sets:

```json
{
  "mcpServers": {
    "unityMCP": {
      "url": "http://localhost:8080/mcp"
    }
  }
}
```

Once connected, AI agents can inspect scenes, manage GameObjects/components, read console output, and run Unity tests through MCP tools.
# geisofanam
