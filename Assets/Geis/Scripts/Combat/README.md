# Geis Combat System

Data-driven combo system with weapon equipping (keys 1-4).

## Setup

1. **Animator**: Run `Tools > Geis > Add Data-Driven Attack (ComboState blend tree)` on AC_Polygon_Masculine_Geis.

2. **If combos show the same animation**: Run `Tools > Geis > Fix Combo blend parameter for normalized 0-1 range`. The blend tree uses thresholds 0-1 over 32 slots; ComboStateBlend (Float) must be set to state/31.

3. **Default ComboData**: Run `Tools > Geis > Create Default ComboData (Unarmed L-L-L)` to create a sample asset at `Assets/Geis/Resources/ComboData_Unarmed.asset`.

4. **Weapon Switcher**: Run `Tools > Geis > Add GeisWeaponSwitcher to PF_PolygonPlayer` to add the component to the player prefab.

5. **Assign in Inspector**: On the player with GeisPlayerAnimationController, assign:
   - `Combo Data`: The ComboData_Unarmed asset (or your custom one).

## Keys

- **1**: Unarmed
- **2**: Knife
- **3**: Sword
- **4**: Bow

## Adding Combo Branches

Edit the GeisComboData ScriptableObject: add transitions (fromState, inputType, toState) and assign clips. Then run `Tools > Geis > Sync GeisComboData clips to Attack blend tree` to copy clips into the animator. No new animator states needed.

**Troubleshooting: Same attack every hit?** The blend tree uses thresholds 0, 1/31, 2/31, ..., 1. Run `Tools > Geis > Fix Combo blend parameter for normalized 0-1 range` to add ComboStateBlend (Float) so the correct clip is selected.
