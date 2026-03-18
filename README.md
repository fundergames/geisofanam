# Geisofanam

Unity project consuming **Funder Core** via the Package Manager.

## Core package

`com.funder.core` is referenced from the package manager (see `Packages/manifest.json`), not embedded:

- **Git:** `https://github.com/fundergames/funder-core.git?path=Packages/com.funder.core#main`

Unity resolves this via the `path` query so the package is loaded from `Packages/com.funder.core` in the funder-core repo. Project code still loads core resources (e.g. `Resources.Load("FunderCore/FGAppConfig")`); ensure those assets exist in your project or are provided by the core package/samples.
