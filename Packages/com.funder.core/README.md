# com.funder.core

Reusable runtime architecture for Funder Games projects.

## Included modules

- `Funder.Core.FSM` - lightweight state machine primitives.
- `Funder.Core.Randoms` - deterministic random hub with named streams.
- `Funder.Core.Services` - simple service locator.
- `Funder.Core.Events` - generic event bus.
- `Funder.Core.Singleton` - singleton attribute/base class/manager.
- `ColorLog` - shared structured logging helpers.

## Usage

In a consuming Unity project `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.funder.core": "https://github.com/fundergames/funder-core.git?path=/Packages/com.funder.core#main"
  }
}
```
