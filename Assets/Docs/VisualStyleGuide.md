# Geis of Annam — Visual Style Guide

Reference for 3D Modeler, Rigger, Animator, and other art-related agents. Ensures consistency across assets.

## Art Direction

*(Placeholder — update with actual art direction when defined)*

### Color Palette

- **Primary**: TBD (fantasy palette)
- **Accent**: TBD
- **Environment**: Natural, earthy tones; avoid oversaturation
- **Characters**: Distinct faction colors; readable silhouettes

### Lighting

- URP standard lighting
- Soft shadows preferred for readability
- Avoid harsh contrasts unless for dramatic effect

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
