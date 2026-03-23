using UnityEngine;
using UnityEditor;
using Geis.Combat;

namespace Geis.Editor
{
    /// <summary>
    /// Setup helpers for Geis combo and weapon systems.
    /// </summary>
    public static class GeisSetupHelper
    {
        private const string PREFAB_PATH = "Assets/RogueDeal/Combat/Prefabs/PF_PolygonPlayer.prefab";
        private const string COMBO_DATA_PATH = "Assets/Geis/Resources/ComboData_Unarmed.asset";

        [MenuItem("Tools/Geis/Add GeisWeaponSwitcher to PF_PolygonPlayer")]
        public static void AddWeaponSwitcherToPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            if (prefab == null)
            {
                Debug.LogError($"[GeisSetupHelper] Prefab not found at {PREFAB_PATH}");
                return;
            }

            var path = AssetDatabase.GetAssetPath(prefab);
            var contents = PrefabUtility.LoadPrefabContents(path);
            var root = contents.transform;

            var character = root.Find("PolygonSyntyCharacter");
            if (character == null)
                character = root;

            var existing = character.GetComponent<GeisWeaponSwitcher>();
            if (existing != null)
            {
                PrefabUtility.UnloadPrefabContents(contents);
                Debug.Log("[GeisSetupHelper] GeisWeaponSwitcher already present.");
                return;
            }

            var switcher = character.gameObject.AddComponent<GeisWeaponSwitcher>();
            PrefabUtility.SaveAsPrefabAsset(contents, path);
            PrefabUtility.UnloadPrefabContents(contents);
            Debug.Log("[GeisSetupHelper] Added GeisWeaponSwitcher to PF_PolygonPlayer.");
        }

        [MenuItem("Tools/Geis/Create Default ComboData (Unarmed L-L-L)")]
        public static void CreateDefaultComboData()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Geis"))
                AssetDatabase.CreateFolder("Assets", "Geis");
            if (!AssetDatabase.IsValidFolder("Assets/Geis/Resources"))
                AssetDatabase.CreateFolder("Assets/Geis", "Resources");

            var existing = AssetDatabase.LoadAssetAtPath<GeisComboData>(COMBO_DATA_PATH);
            if (existing != null)
            {
                Debug.Log("[GeisSetupHelper] Default ComboData already exists at " + COMBO_DATA_PATH);
                return;
            }

            var comboData = ScriptableObject.CreateInstance<GeisComboData>();

            var transitions = new System.Collections.Generic.List<GeisComboTransition>
            {
                new GeisComboTransition { fromState = 0, inputType = GeisComboInputType.Light, toState = 1 },
                new GeisComboTransition { fromState = 1, inputType = GeisComboInputType.Light, toState = 2 },
                new GeisComboTransition { fromState = 2, inputType = GeisComboInputType.Light, toState = 0 },
                new GeisComboTransition { fromState = 1, inputType = GeisComboInputType.Heavy, toState = 3 },
                new GeisComboTransition { fromState = 2, inputType = GeisComboInputType.Heavy, toState = 4 },
                new GeisComboTransition { fromState = 3, inputType = GeisComboInputType.Light, toState = 0 },
                new GeisComboTransition { fromState = 3, inputType = GeisComboInputType.Heavy, toState = 0 },
                new GeisComboTransition { fromState = 4, inputType = GeisComboInputType.Light, toState = 0 },
                new GeisComboTransition { fromState = 4, inputType = GeisComboInputType.Heavy, toState = 0 }
            };

            SerializedObject so = new SerializedObject(comboData);
            so.FindProperty("transitions").ClearArray();
            for (int i = 0; i < transitions.Count; i++)
            {
                so.FindProperty("transitions").InsertArrayElementAtIndex(i);
                var elem = so.FindProperty("transitions").GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("fromState").intValue = transitions[i].fromState;
                elem.FindPropertyRelative("inputType").enumValueIndex = (int)transitions[i].inputType;
                elem.FindPropertyRelative("toState").intValue = transitions[i].toState;
            }

            var attack1 = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/RogueDeal/Combat/Animations/Attack_1.anim")
                ?? AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Synty/AnimationBaseLocomotion/Samples/Animations/Polygon/Masculine/Combat/Melee_1H_Attack_Chop.anim");
            var attack2 = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/RogueDeal/Combat/Animations/Attack_2.anim")
                ?? attack1;
            var attack3 = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/RogueDeal/Combat/Animations/Attack_3.anim")
                ?? attack1;

            so.FindProperty("clips").ClearArray();
            for (int i = 0; i < 5; i++)
            {
                so.FindProperty("clips").InsertArrayElementAtIndex(i);
                var clip = i == 0 ? attack1 : i == 1 ? attack2 : i == 2 ? attack3 : i == 3 ? attack3 : attack3;
                so.FindProperty("clips").GetArrayElementAtIndex(i).objectReferenceValue = clip;
            }
            so.FindProperty("fallbackClip").objectReferenceValue = attack1;
            so.FindProperty("cancelWindowStart").floatValue = 0.2f;
            so.FindProperty("cancelWindowEnd").floatValue = 0.7f;
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(comboData, COMBO_DATA_PATH);
            AssetDatabase.SaveAssets();
            Debug.Log("[GeisSetupHelper] Created default ComboData at " + COMBO_DATA_PATH);
        }
    }
}
