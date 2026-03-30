# Soul realm weapon abilities (revised)

## Scope

- **Harp-Bow (Bow weapon definition)**: Soul Marking (Q), Path Reveal (F).
- **Lyre Sword (Emberblade)**: Passive resonance on hits; Wave Release (Q); secondary slot empty.
- **Dagger-Flute (Aetherstorm)**: Object Blink (Q), Phase Shift Object (F).

## Integration

- Ability assets: `Assets/Geis/SoulRealm/Abilities/`.
- `Player` prefab: `LyreResonanceMeter`, `SoulRealmWeaponAbilityController` (wired to `SoulRealmWeaponAbilities` input asset and `GeisCameraController`).
- Weapon definitions reference soul abilities and `buildsLyreResonance` on Emberblade only.

## Scene setup

- **Soul Marking**: Add `SoulMarkTarget` (or `ISoulMarkable`) to props/enemies; raycast uses camera center.
- **Path Reveal**: Add `SoulPathRevealElement` to hidden hints; optional `revealEntireScene` on the Path Reveal asset.
- **Lyre**: Land hits with Emberblade to fill resonance; Wave Release needs enemies in the forward sphere cast.
- **Dagger**: Add `SoulBlinkable` (two pose transforms) and `SoulPhaseShiftable` (colliders + optional ghost visual); configure **phased layer** in Project Settings collision matrix.

## Status

Engineer integration complete; tune ranges, layers, and VFX in-editor.
