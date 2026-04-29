# Geisofanam

Unity project consuming **Funder Core** via the Package Manager.

## CI/CD: GitHub Actions WebGL builds to AWS S3

This repository now includes a GitHub Actions workflow at:

- `.github/workflows/unity-webgl-s3.yml`

It does the following:

1. Builds Unity WebGL on every push to `main` (and manual run via `workflow_dispatch`)
2. Uploads a build artifact in GitHub Actions
3. Deploys to S3 in two locations:
   - `builds/<timestamp-run-sha>/` (immutable build history)
   - `latest/` (always the newest deploy)
4. Prunes old build folders so only the last **N** builds are kept

### Required GitHub repository configuration

Set these repository **Variables**:

- `AWS_S3_BUCKET` (required): target bucket name
- `AWS_REGION` (optional, default `us-east-1`)
- `AWS_S3_PREFIX` (optional): prefix/folder under bucket (for example `geis/webgl`)
- `AWS_S3_KEEP_BUILDS` (optional, default `10`): how many historical builds to retain
- `AWS_ROLE_TO_ASSUME` (optional but recommended): IAM role ARN for GitHub OIDC

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
- The workflow automatically deletes older `builds/` folders once the limit is exceeded.
- Optional: also add an S3 lifecycle rule to expire very old objects as a safety net.

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
