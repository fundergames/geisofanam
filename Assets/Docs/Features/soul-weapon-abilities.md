# Soul realm weapon abilities (revised)

> Canonical state fields for this feature live in `Assets/Docs/FeatureRegistry.json`.
> If this feature is moved back into full role-based pipeline flow, migrate it to the standard frontmatter template.

## Scope

- **Harp-Bow (Bow weapon definition)**: Soul Marking (Q), Path Reveal (F).
- **Lyre Sword (Emberblade)**: Passive resonance on hits; Wave Release (Q); secondary slot empty.
- **Dagger-Flute (Aetherstorm)**: Object Blink (Q), Phase Shift Object (F).

## Integration

- Ability assets: `Assets/Geis/SoulRealm/Abilities/`.
- `Player` prefab: `LyreResonanceMeter`, `SoulRealmWeaponAbilityController` (wired to `GeisControls` input asset for the `SoulRealmWeapon` map and `GeisCameraController`).
- Weapon definitions reference soul abilities and `buildsLyreResonance` on Emberblade only.

## Scene setup

- **Soul Marking**: Add `SoulMarkTarget` (or `ISoulMarkable`) to props/enemies; raycast uses camera center.
- **Path Reveal**: Add `SoulPathRevealElement` to hidden hints; optional `revealEntireScene` on the Path Reveal asset.
- **Lyre**: Land hits with Emberblade to fill resonance; Wave Release needs enemies in the forward sphere cast.
- **Dagger**: Add `SoulBlinkable` (two pose transforms) and `SoulPhaseShiftable` (colliders + optional ghost visual). During Object Blink manipulation the soul body should stay frozen while `Move` repositions the target, keyboard `UpArrow` / `DownArrow` or gamepad `D-pad Up` / `D-pad Down` move it vertically, and holding `Aim` / gamepad left trigger turns `Move` into rotation input for socket alignment. Phase Shift now uses hold input in either realm to transfer a target object so it stays solid only in the player's current realm and ethereal in the opposite realm until shifted back; ethereal props stay targetable and non-blocking by switching their colliders to triggers.

## Status

Engineer integration complete; tune ranges, layers, and VFX in-editor.
