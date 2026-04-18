---
title: "Boss: Giant Soul Warden — Phase structure + Phase 3 crit-spot loop"
status: spec_ready
current_owner: Engineer
next_owner: QA
mode: production
concept_locked: true
selected_variant: "phase3_dual_realm_crit_loop"

approvals:
  design: approved
  architect: approved
  architect_review: approved
  modeling: n/a
  engineering: in_progress
  code_review: pending
  qa: pending
  video_demo: pending

blocking_issues: []
assumptions:
  - Phase 1/2 remain as-is (slam loop + existing soul shields + existing crit window).
  - Phase 3 is the final phase and ends only when the boss is killed.
  - Phase 3 starts by breaking each fist's existing soul shield (like phase 2) to pin the fists; no additional fist shield is added.
  - Phase 3 uses soul realm for the fist shields + soul-crit shield, and physical realm for fist damage + physical crit/eye targets.
risks:
  - Prefab scene hookups for new shield/eye references may be missing; code must fail-soft with clear logs.
  - Timer/realm-freeze interactions can desync if not using RealmSimulation groups.

version: 2
last_updated_by: gpt-5.2
last_updated_at: 2026-04-15
change_summary: "Document Phase 1/2 timed reset structure and align Phase 3 to beams + dual crit-spot hits (physical then soul)."
---

## Design Brief (Design)

This document captures the intended **phase structure** (1–3), with Phase 3 as the finale.

### Phase 1

- **Physical Fist 1 Slam**: take damage to **stun**.
- **Physical Fist 2 Slam**: take damage to **stun**.
- **Timer**: if both fists are not stunned in time, **restart Phase 1 from the beginning**.
- **Soul Realm Crit Spot Shield**: deal **shield damage** (in Soul Realm) to **end Phase 1**.

### Phase 2

- **Timer (stun gate)**: timed for both fists to be **stunned**, otherwise **restart Phase 2**.
- **Soul Realm Fist 1 Slam**: destroy its **soul shield** to stun/pin.
- **Soul Realm Fist 2 Slam**: destroy its **soul shield** to stun/pin.
- **Timer (completion gate)**: timed to destroy **both fists**, **physical crit shield**, and **soul crit shield**; otherwise **restart Phase 2**.
- **Physical Eye Beams x2**
  - **Physical Realm Fist 1 & Fist 2**: deal damage to destroy fists
  - **Physical Realm Crit Shield**: destroy shield
- **Soul Realm Crit Shield**: deal shield damage to **end Phase 2**.

### Phase 3 (finale)

- **Timer (stun gate)**: timed for both fists to be **stunned**, otherwise **restart Phase 3**.
- **Soul Realm Fist 1 Slam**: destroy its **soul shield** to stun/pin.
- **Soul Realm Fist 2 Slam**: destroy its **soul shield** to stun/pin.
- **Timer (completion gate)**: timed to destroy **both fists**, **physical crit shield**, **soul crit shield**, and both **crit-spot hits**; otherwise **restart Phase 3**.
- **Physical Eye Beams x2**
  - **Physical Realm Fist 1 & Fist 2**: deal damage to destroy fists
  - **Physical Realm Crit Shield**: destroy shield
- **Soul Realm Eye Beams x3**
  - **Soul Realm Crit Shield**: destroy shield
- **Physical Realm Crit Spot**: deal damage (progress gate)
- **Soul Realm Crit Spot**: deal damage for **win**

### Phase 3 implementation detail (loop sequence)

Phase 3 becomes a **repeatable dual-realm puzzle-combat loop**, matching the intended phase structure:

- **Step A (Soul)**: **Fist 1 slam** → destroy its **soul shield** (in soul realm) to **stun/pin** it.
- **Step B (Soul)**: **Fist 2 slam** → destroy its **soul shield** (in soul realm) to **stun/pin** it.
- **Step C (Physical)**: **Eye beams x2** (survival check).
- **Step D (Physical)**: With both fists pinned, destroy **Physical Fist 1** and **Physical Fist 2**.
- **Step E (Physical)**: Destroy **Physical Realm Crit Shield**.
- **Step F (Soul)**: **Soul Realm eye beams x3** (survival check).
- **Step G (Soul)**: Destroy **Soul Realm Crit Shield**.
- **Step H (Physical)**: Deal damage to the **Physical Realm Crit Spot** (progress gate).
- **Step I (Soul)**: Deal damage to the **Soul Realm Crit Spot** for **win**.

Primary intent: force quick realm swaps and target prioritization under time pressure, with a readable “success chain”.

**Handoff to Architect**: [x] Complete

## Architect Spec (Architect)

### Technical approach

- Add a dedicated `IBossPhase` implementation for phase 3 (`GiantBossPhase3DualRealmLoop`), leaving phases 1/2 on `GiantBossConfiguredPhase`.
- Introduce small, reusable shield targets:
  - **Physical shield** target: a `CombatEntity`-backed component that consumes physical damage and raises an event on break.
  - **Soul shield** target: reuse existing `BossPartShield`-style pattern (ghost input + soul projectile sink), but attached to the **crit spot** rather than a fist.
- Phase 3 logic uses **realm-scoped time**:
  - Soul-shield countdown uses `RealmSimulationGroup.Soul`
  - Physical countdown uses `RealmSimulationGroup.Physical`
  - Universal delays use `RealmSimulationGroup.Universal`
- Phase reset must:
  - Clear pinned state on fists (restore normal slam loop)
  - Hide/restore both crit shields and eye vulnerability
  - Stop any in-flight beam sequences

### Component hierarchy (expected)

- `GiantBossController` (existing)
  - `BossPart` (Right hand)
    - `BossPartShield` (existing; soul-only shield used to pin)
  - `BossPart` (Left hand)
    - `BossPartShield` (existing; soul-only shield used to pin)
  - `CritSpot` (existing)
    - `Phase3SoulCritShield` (new; soul-only shield)
    - `Phase3PhysicalCritShieldTarget` (new; physical-only shield)
  - *(Optional / legacy)* `Phase3EyeTarget` (if retained) is no longer the win condition in this variant.

### Integration points

- Uses existing `BossPart.OnPartBroken`, `BossPart.SetState`, and `CritSpot.SetVulnerable` patterns.
- Uses `SoulRealmManager` and `RealmSimulation` helpers for correct freeze behavior.

**Handoff to Engineer**: [x] Complete

## Integration / Implementation Plan (Engineer)

- Add new phase-3 class and route phase index 3 to it from `GiantBossController`.
- Add new shield/target components:
  - `PhysicalDamageShieldTarget` (generic physical shield w/ max HP + OnBroken event).
  - `SoulDamageShieldTarget` (generic soul-only shield w/ ghost input + optional soul projectile sink).
- Add phase-3 references + tuning fields:
  - `xSoulShieldSeconds`, `yPhysicalCleanupSeconds`, `zTrackingBeamsCount`, plus beam cadence/damage.
- Ensure phase resets are safe when objects are missing; log once and hard-reset to “start over”.

**Handoff to QA**: [ ] Complete

## QA Checklist (QA)

- [ ] In phase 3, breaking each fist's **soul shield** pins that fist (does not lift/recover) and makes it damageable.
- [ ] After both fists are pinned, **physical eye beams x2** run before fists are destroyed.
- [ ] Destroying both fists reveals **physical crit shield**; breaking it progresses.
- [ ] **Soul eye beams x3** run; then **soul crit shield** becomes breakable in soul realm.
- [ ] After soul crit shield breaks, **physical crit spot** must take damage to progress.
- [ ] Final win requires **soul crit spot** damage; boss dies.
