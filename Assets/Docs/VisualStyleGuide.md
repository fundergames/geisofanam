# Geis of Annam — Visual Style Guide

Reference for 3D Modeler, Rigger, Animator, and other art-related agents. Ensures consistency across assets.

## Art Direction

Stylized fantasy with readable gameplay-first silhouettes.

- Prioritize readability at gameplay camera distance over surface micro-detail.
- Favor bold primary forms and restrained secondary detail.
- Preserve faction identity through color accents and shape language, not noisy materials.

### Color Palette

- **Primary**: Muted natural palette (forest greens, stone grays, desaturated browns)
- **Accent**: Controlled magical accents (teal/cyan for soul-aligned effects, warm amber for physical/combat highlights)
- **Environment**: Natural, earthy tones; avoid oversaturation
- **Characters**: Distinct faction colors; readable silhouettes

### Lighting

- URP standard lighting
- Soft shadows preferred for readability
- Avoid harsh contrasts unless for dramatic effect
- Keep gameplay-critical actors readable in mid-contrast conditions (avoid crushed blacks on characters)

## Technical Specifications

### Poly Counts

| Asset Type | Target Tris | Max Tris |
|------------|-------------|----------|
| Characters | 5,000–15,000 | 25,000 |
| Props | 500–2,000 | 5,000 |
| Environment | 1,000–10,000 per piece | 20,000 |

### Texture Style

- PBR workflow (albedo, normal, metallic/smoothness)
- Resolution: 512–2048 depending on asset importance
- Consistent texel density across similar asset types

### Rig Requirements

- **Characters**: Humanoid rig preferred for Animation retargeting
- **Props**: No rig unless animated
- **Creatures**: Humanoid or Generic depending on complexity

## Reference Assets

- `RPGTinyHeroWavePBR/` — Character art style reference
- Match proportion and silhouette language of existing characters

## Style Adherence Checklist (for Modeler)

- [ ] Poly count within spec
- [ ] Materials use PBR workflow
- [ ] Color palette consistent with guide
- [ ] Rig type specified (Humanoid/Generic)
- [ ] Silhouette readable and distinct
