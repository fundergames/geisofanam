using UnityEngine;
using UnityEditor;
using RogueDeal.Combat.Core.Data;
using RogueDeal.Combat.Core.Effects;
using RogueDeal.Combat.Core.Cooldowns;
using RogueDeal.Combat.Core.Targeting;
using RogueDeal.Player;

namespace RogueDeal.Combat.Editor
{
    /// <summary>
    /// Editor helper to create test assets for the combat system.
    /// Use: Tools > Combat System > Create Test Assets
    /// </summary>
    public class CombatSystemTestHelper : EditorWindow
    {
        public static void CreateTestAssets()
        {
            string basePath = "Assets/Geis/Resources/Combat/TestAssets/";
            
            // Create directory if it doesn't exist
            if (!AssetDatabase.IsValidFolder(basePath))
            {
                string[] folders = basePath.TrimEnd('/').Split('/');
                string currentPath = folders[0];
                for (int i = 1; i < folders.Length; i++)
                {
                    string newPath = currentPath + "/" + folders[i];
                    if (!AssetDatabase.IsValidFolder(newPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, folders[i]);
                    }
                    currentPath = newPath;
                }
            }
            
            // Create test weapon
            var fireSword = CreateWeapon("FireSword", basePath);
            fireSword.baseDamage = 50f;
            fireSword.damageTypeMultiplierArray = new DamageTypeMultiplier[]
            {
                new DamageTypeMultiplier { damageType = DamageType.Physical, multiplier = 1.0f },
                new DamageTypeMultiplier { damageType = DamageType.Fire, multiplier = 1.2f }
            };
            EditorUtility.SetDirty(fireSword);
            
            // Create test effects
            var physicalDamage = CreateDamageEffect("PhysicalDamage", basePath);
            physicalDamage.baseDamage = 50f;
            physicalDamage.damageType = DamageType.Physical;
            physicalDamage.scalingStat = StatType.Attack;
            physicalDamage.scalingMultiplier = 1f;
            EditorUtility.SetDirty(physicalDamage);
            
            var fireDamage = CreateDamageEffect("FireDamage", basePath);
            fireDamage.baseDamage = 20f;
            fireDamage.damageType = DamageType.Fire;
            fireDamage.scalingStat = StatType.Magic;
            fireDamage.scalingMultiplier = 1f;
            EditorUtility.SetDirty(fireDamage);
            
            var burnStatus = CreateStatusEffect("Burn", basePath);
            burnStatus.statusType = StatusEffectType.Burning;
            burnStatus.stacks = 1;
            burnStatus.damagePerStack = 5;
            burnStatus.duration = 3;
            EditorUtility.SetDirty(burnStatus);
            
            // Create multi-effect (Fire Slash = Physical + Fire + Burn)
            var fireSlashEffect = CreateMultiEffect("FireSlashEffect", basePath);
            fireSlashEffect.effects = new BaseEffect[] { physicalDamage, fireDamage, burnStatus };
            EditorUtility.SetDirty(fireSlashEffect);
            
            // Create test action
            var fireSlashAction = CreateCombatAction("FireSlash", basePath);
            fireSlashAction.actionName = "Fire Slash";
            fireSlashAction.description = "A fiery sword attack that deals physical and fire damage, and applies burn";
            fireSlashAction.animationTrigger = "FireSlash";
            fireSlashAction.effects = new BaseEffect[] { fireSlashEffect };
            fireSlashAction.cooldownConfig = new CooldownConfiguration
            {
                cooldownType = CooldownType.TurnBased,
                turnCooldown = 3
            };
            EditorUtility.SetDirty(fireSlashAction);
            
            // Create targeting strategies
            var singleTarget = CreateSingleTargetSelector("SingleTarget", basePath);
            EditorUtility.SetDirty(singleTarget);
            
            // Create combat profile
            var warriorProfile = CreateCombatProfile("Warrior", basePath);
            warriorProfile.combatRange = CombatRange.Melee;
            warriorProfile.engagementDistance = 1.5f;
            warriorProfile.requiresLineOfSight = true;
            EditorUtility.SetDirty(warriorProfile);
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"✅ Created test assets in {basePath}");
            Debug.Log("Created:");
            Debug.Log("  - FireSword (Weapon)");
            Debug.Log("  - PhysicalDamage, FireDamage (Damage Effects)");
            Debug.Log("  - Burn (Status Effect)");
            Debug.Log("  - FireSlashEffect (Multi Effect)");
            Debug.Log("  - FireSlash (Combat Action)");
            Debug.Log("  - SingleTarget (Targeting Strategy)");
            Debug.Log("  - Warrior (Combat Profile)");
        }
        
        private static Weapon CreateWeapon(string name, string path)
        {
            var weapon = ScriptableObject.CreateInstance<Weapon>();
            weapon.weaponName = name;
            AssetDatabase.CreateAsset(weapon, $"{path}{name}.asset");
            return weapon;
        }
        
        private static DamageEffect CreateDamageEffect(string name, string path)
        {
            var effect = ScriptableObject.CreateInstance<DamageEffect>();
            effect.effectName = name;
            AssetDatabase.CreateAsset(effect, $"{path}Effect_{name}.asset");
            return effect;
        }
        
        private static StatusEffect CreateStatusEffect(string name, string path)
        {
            var effect = ScriptableObject.CreateInstance<StatusEffect>();
            effect.effectName = name;
            AssetDatabase.CreateAsset(effect, $"{path}Effect_{name}.asset");
            return effect;
        }
        
        private static MultiEffect CreateMultiEffect(string name, string path)
        {
            var effect = ScriptableObject.CreateInstance<MultiEffect>();
            effect.effectName = name;
            AssetDatabase.CreateAsset(effect, $"{path}Effect_{name}.asset");
            return effect;
        }
        
        private static CombatAction CreateCombatAction(string name, string path)
        {
            var action = ScriptableObject.CreateInstance<CombatAction>();
            action.actionName = name;
            AssetDatabase.CreateAsset(action, $"{path}Action_{name}.asset");
            return action;
        }
        
        private static SingleTargetSelector CreateSingleTargetSelector(string name, string path)
        {
            var selector = ScriptableObject.CreateInstance<SingleTargetSelector>();
            selector.strategyName = name;
            AssetDatabase.CreateAsset(selector, $"{path}Targeting_{name}.asset");
            return selector;
        }
        
        private static CombatProfile CreateCombatProfile(string name, string path)
        {
            var profile = ScriptableObject.CreateInstance<CombatProfile>();
            profile.profileName = name;
            AssetDatabase.CreateAsset(profile, $"{path}Profile_{name}.asset");
            return profile;
        }
        
        [MenuItem("Tools/Combat System/Open Test Window")]
        public static void ShowWindow()
        {
            GetWindow<CombatSystemTestHelper>("Combat System Tests");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Combat System Test Helper", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            if (GUILayout.Button("Create Test Assets", GUILayout.Height(30)))
            {
                CreateTestAssets();
            }
            
            GUILayout.Space(10);
            GUILayout.Label("Instructions:", EditorStyles.boldLabel);
            GUILayout.Label("1. Click 'Create Test Assets' to generate test ScriptableObjects");
            GUILayout.Label("2. Add CombatSystemTester component to a GameObject in a scene");
            GUILayout.Label("3. Run the scene to execute tests");
            GUILayout.Label("4. Check the Test Results field in the inspector");
        }
    }
}

