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
# geisofanam
