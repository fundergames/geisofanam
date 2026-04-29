# Geisofanam

Unity project consuming **Funder Core** via the Package Manager.

## CI/CD: Modular WebGL release management (dev/staging/production)

This repository now includes modular GitHub Actions workflows and a reusable script:

- `.github/workflows/unity-webgl-s3.yml`
- `.github/workflows/promote-webgl-release.yml`
- `.github/workflows/rollback-webgl-release.yml`
- `.github/scripts/s3-release-manager.sh`

### Release model

- Immutable build artifacts are published to:
  - `builds/<build-id>/`
- Environment pointers are managed via manifests:
  - `releases/dev/current.json`
  - `releases/staging/current.json`
  - `releases/production/current.json`
- Previous pointers are retained:
  - `releases/<env>/previous.json`
- Promotion history is retained:
  - `releases/<env>/history/*.json`

Optional channel copy (for simple hosting/CDN setups):

- `channels/<env>/latest/` (enabled only when `AWS_RELEASE_COPY_TO_CHANNEL=true`)

### Workflows

1. **Build and publish** (`unity-webgl-s3.yml`)
   - Builds Unity WebGL
   - Publishes immutable build to `builds/<build-id>/`
   - Prunes old builds while protecting currently referenced releases
2. **Promote** (`promote-webgl-release.yml`)
   - Promotes a specific `build_id` to `dev`, `staging`, or `production`
   - Updates `current.json`, shifts prior `current` to `previous.json`, appends history
3. **Rollback** (`rollback-webgl-release.yml`)
   - Rolls back selected environment to prior history entry
   - Updates `current.json`, keeps rollback in history trail

### Suggested flow

- Developers publish builds from feature branches/manual runs, then promote to `dev`
- QA promotes validated build from `dev` to `staging`
- Release manager promotes approved staging build to `production`
- If production issue appears, trigger rollback workflow for `production`

### Required GitHub repository configuration

Set these repository **Variables**:

- `AWS_S3_BUCKET` (required): target bucket name
- `AWS_REGION` (optional, default `us-east-1`)
- `AWS_S3_PREFIX` (optional): prefix/folder under bucket (for example `geis/webgl`)
- `AWS_S3_KEEP_BUILDS` (optional, default `10`): how many historical builds to retain
- `AWS_ROLE_TO_ASSUME` (optional but recommended): IAM role ARN for GitHub OIDC
- `AWS_RELEASE_ENVIRONMENTS` (optional, default `dev,staging,production`): environments to protect during prune
- `RELEASE_COPY_TO_CHANNEL` (optional, default `false`): copy promoted build to `channels/<env>/latest/`

Set either:

- **Recommended**: OIDC role via `AWS_ROLE_TO_ASSUME`, or
- **Fallback secrets**:
  - `AWS_ACCESS_KEY_ID`
  - `AWS_SECRET_ACCESS_KEY`

Unity build credentials (required by game-ci):

- `UNITY_LICENSE` (preferred) or license credentials used by your plan
- `UNITY_EMAIL` / `UNITY_PASSWORD` / `UNITY_SERIAL` (if applicable)

### Cost control notes

- Keep `AWS_S3_KEEP_BUILDS` low (for example `3` to `10`) to reduce storage.
- Build prune automatically deletes old `builds/` folders while preserving builds referenced by release manifests.
- Optional: also add an S3 lifecycle rule to expire very old objects as a safety net.

### Frontend integration contract

Your frontend (Railway or any other host) should read a release manifest, not hardcode `latest`:

- Dev: `https://<cdn>/<prefix>/releases/dev/current.json`
- Staging: `https://<cdn>/<prefix>/releases/staging/current.json`
- Prod: `https://<cdn>/<prefix>/releases/production/current.json`

Then load the game from `buildPrefix` in the manifest. This keeps frontend and game deployments decoupled and makes promotion/rollback instant without frontend redeploys.

### Reuse in another repo

This setup is product-agnostic:

- Move `.github/scripts/s3-release-manager.sh` and the workflows to another repo
- Change Unity build details only (project specifics and credentials)
- Keep the same S3 release folder contract and promote/rollback workflows

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
