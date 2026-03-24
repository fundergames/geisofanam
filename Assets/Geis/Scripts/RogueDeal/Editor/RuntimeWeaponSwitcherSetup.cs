using UnityEngine;
using UnityEditor;
using RogueDeal.Combat.Presentation;
using RogueDeal.Combat.Core.Data;

namespace RogueDeal.Editor
{
    /// <summary>
    /// Editor utilities for setting up RuntimeWeaponSwitcher with 1-handed weapons.
    /// Creates Weapon assets (with prefab refs) and assigns them to the switcher.
    /// </summary>
    public static class RuntimeWeaponSwitcherSetup
    {
        private const string WeaponDataFolder = "Assets/RogueDeal/Combat/Data/Weapons";

        // Use source prefabs (not variants) - variants have PrefabInstance root which causes InvalidCastException on Instantiate
        private static readonly (string PrefabPath, string WeaponName)[] OneHandedWeapons =
        {
            ("Assets/KayKit/Characters/KayKit - Adventurers (for Unity)/Prefabs/Accessories/sword_1handed.prefab", "Sword"),
            ("Assets/KayKit/Characters/KayKit - Adventurers (for Unity)/Prefabs/Accessories/dagger.prefab", "Dagger"),
            ("Assets/KayKit/Characters/KayKit - Adventurers (for Unity)/Prefabs/Accessories/axe_1handed.prefab", "Axe"),
            ("Assets/KayKit/Characters/KayKit - Adventurers (for Unity)/Prefabs/Accessories/crossbow_1handed.prefab", "Crossbow"),
            ("Assets/KayKit/Characters/KayKit - Adventurers (for Unity)/Prefabs/Accessories/wand.prefab", "Wand"),
        };

        [MenuItem("Funder Games/Rogue Deal/Setup Runtime Weapon Switcher on Player Prefab")]
        public static void SetupOnPlayerPrefab()
        {
            var prefabPath = "Assets/RogueDeal/Combat/Prefabs/Player.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[RuntimeWeaponSwitcherSetup] Player prefab not found at {prefabPath}");
                return;
            }

            var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            var switcher = prefabRoot.GetComponent<RuntimeWeaponSwitcher>();
            if (switcher == null)
            {
                switcher = prefabRoot.AddComponent<RuntimeWeaponSwitcher>();
                Debug.Log("[RuntimeWeaponSwitcherSetup] Added RuntimeWeaponSwitcher component");
            }

            PopulateWeaponSlots(switcher);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);

            Debug.Log("[RuntimeWeaponSwitcherSetup] ✅ Player prefab updated. Keys 1-5: Sword, Dagger, Axe, Crossbow, Wand.");
        }

        [MenuItem("Funder Games/Rogue Deal/Setup Runtime Weapon Switcher on Selected Object")]
        public static void SetupOnSelectedObject()
        {
            var go = Selection.activeGameObject;
            if (go == null)
            {
                Debug.LogWarning("[RuntimeWeaponSwitcherSetup] No GameObject selected. Select the Player or character in the scene.");
                return;
            }

            var switcher = go.GetComponent<RuntimeWeaponSwitcher>();
            if (switcher == null)
            {
                switcher = go.AddComponent<RuntimeWeaponSwitcher>();
                Debug.Log("[RuntimeWeaponSwitcherSetup] Added RuntimeWeaponSwitcher component to " + go.name);
            }

            PopulateWeaponSlots(switcher);

            if (PrefabUtility.IsPartOfAnyPrefab(go))
            {
                EditorUtility.SetDirty(go);
                PrefabUtility.RecordPrefabInstancePropertyModifications(go);
            }

            Debug.Log("[RuntimeWeaponSwitcherSetup] ✅ Weapon slots populated. Keys 1-5: Sword, Dagger, Axe, Crossbow, Wand.");
        }

        private static void PopulateWeaponSlots(RuntimeWeaponSwitcher switcher)
        {
            var weapons = new Weapon[OneHandedWeapons.Length];
            for (int i = 0; i < OneHandedWeapons.Length; i++)
            {
                weapons[i] = GetOrCreateWeaponAsset(OneHandedWeapons[i].PrefabPath, OneHandedWeapons[i].WeaponName);
            }

            var so = new SerializedObject(switcher);
            var slotsProperty = so.FindProperty("weaponSlots");
            slotsProperty.arraySize = weapons.Length;
            for (int i = 0; i < weapons.Length; i++)
            {
                slotsProperty.GetArrayElementAtIndex(i).objectReferenceValue = weapons[i];
            }
            so.ApplyModifiedProperties();
        }

        private static Weapon GetOrCreateWeaponAsset(string prefabPath, string weaponName)
        {
            if (!AssetDatabase.IsValidFolder("Assets/RogueDeal/Combat/Data"))
                AssetDatabase.CreateFolder("Assets/RogueDeal/Combat", "Data");
            if (!AssetDatabase.IsValidFolder(WeaponDataFolder))
                AssetDatabase.CreateFolder("Assets/RogueDeal/Combat/Data", "Weapons");

            var assetPath = $"{WeaponDataFolder}/Weapon_{weaponName.Replace(" ", "")}.asset";
            var weapon = AssetDatabase.LoadAssetAtPath<Weapon>(assetPath);
            if (weapon == null)
            {
                weapon = ScriptableObject.CreateInstance<Weapon>();
                weapon.weaponName = weaponName;
                AssetDatabase.CreateAsset(weapon, assetPath);
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var weaponSo = new SerializedObject(weapon);
            weaponSo.FindProperty("weaponPrefab").objectReferenceValue = prefab;
            weaponSo.FindProperty("weaponName").stringValue = weaponName;
            weaponSo.ApplyModifiedProperties();
            EditorUtility.SetDirty(weapon);
            AssetDatabase.SaveAssetIfDirty(weapon);

            return weapon;
        }
    }
}
