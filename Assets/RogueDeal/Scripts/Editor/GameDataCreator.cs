using RogueDeal.Combat;
using RogueDeal.Items;
using RogueDeal.Enemies;
using RogueDeal.Levels;
using RogueDeal.Player;
using UnityEditor;
using UnityEngine;

namespace RogueDeal.Editor
{
    public class GameDataCreator : EditorWindow
    {
        [MenuItem("Funder Games/Rogue Deal/Create Example Data")]
        public static void ShowWindow()
        {
            GetWindow<GameDataCreator>("Game Data Creator");
        }

        private void OnGUI()
        {
            GUILayout.Label("Example Data Creator", EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("Create Example Warrior Class", GUILayout.Height(40)))
            {
                CreateWarriorClass();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Create Example Fire Sword", GUILayout.Height(40)))
            {
                CreateFireSword();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Create Test Enemy (Goblin)", GUILayout.Height(40)))
            {
                CreateTestEnemy();
            }
            
            GUILayout.Space(5);

            if (GUILayout.Button("Create Test Level", GUILayout.Height(40)))
            {
                CreateTestLevel();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("CREATE ALL", GUILayout.Height(50)))
            {
                CreateWarriorClass();
                CreateFireSword();
                CreateTestEnemy();
                CreateTestLevel();
                
                Debug.Log("=== ALL EXAMPLE DATA CREATED! ===");
            }

            GUILayout.Space(10);
            GUILayout.Label("This will create example ScriptableObject data in:", EditorStyles.helpBox);
            GUILayout.Label("/Assets/RogueDeal/Resources/Data/", EditorStyles.miniLabel);
        }

        private void CreateWarriorClass()
        {
            string path = "Assets/RogueDeal/Resources/Data/Classes";
            if (!AssetDatabase.IsValidFolder(path))
            {
                System.IO.Directory.CreateDirectory(path);
            }

            var warrior = ScriptableObject.CreateInstance<ClassDefinition>();
            warrior.classType = CharacterClass.Warrior;
            warrior.displayName = "Warrior";
            warrior.description = "High health and defense, bonus to straight hands";
            
            warrior.baseStats = new CharacterStats
            {
                maxHealth = 150,
                currentHealth = 150,
                attack = 15,
                damage = 8,
                magic = 3,
                defense = 10,
                damageMultiplier = 1f,
                defenseMultiplier = 1f
            };

            warrior.healthPerLevel = 15;
            warrior.attackPerLevel = 3;
            warrior.damagePerLevel = 2;
            warrior.magicPerLevel = 1;
            warrior.defensePerLevel = 2;

            warrior.abilities.Add(new ClassAbility
            {
                abilityName = "Warrior's Fortitude",
                description = "+20% max health",
                requiredLevel = 1,
                isPassive = true,
                healthModifier = 0.2f
            });

            warrior.abilities.Add(new ClassAbility
            {
                abilityName = "Straight Mastery",
                description = "+20% bonus damage",
                requiredLevel = 1,
                isPassive = true,
                bonusDamageMultiplier = 1.2f
            });

            warrior.attackMappings.Add(new ClassAttackMapping
            {
                attackName = "Quick Strike",
                numberOfHits = 1,
                damageMultiplier = 1f,
                damageType = DamageType.Weapon
            });

            warrior.attackMappings.Add(new ClassAttackMapping
            {
                attackName = "Double Strike",
                numberOfHits = 2,
                timeBetweenHits = 0.3f,
                damageMultiplier = 1f,
                damageType = DamageType.Weapon
            });

            warrior.attackMappings.Add(new ClassAttackMapping
            {
                attackName = "Blade Flurry",
                numberOfHits = 5,
                timeBetweenHits = 0.2f,
                damageMultiplier = 1.2f,
                damageType = DamageType.Weapon,
                enableScreenShake = true
            });

            warrior.attackMappings.Add(new ClassAttackMapping
            {
                attackName = "Legendary Rampage",
                numberOfHits = 10,
                timeBetweenHits = 0.1f,
                damageMultiplier = 2f,
                damageType = DamageType.Weapon,
                enableScreenShake = true,
                screenShakeIntensity = 0.5f
            });

            warrior.xpCurve = AnimationCurve.EaseInOut(1, 100, 50, 10000);
            
            var doubleSwordAnimData = AssetDatabase.LoadAssetAtPath<ClassAnimatorData>("Assets/RogueDeal/Resources/Data/Animators/DoubleSword_AnimatorData.asset");
            if (doubleSwordAnimData != null)
            {
                warrior.animatorData = doubleSwordAnimData;
                Debug.Log("[GameDataCreator] Assigned DoubleSword animator data to Warrior class");
            }
            else
            {
                Debug.LogWarning("[GameDataCreator] Could not find DoubleSword_AnimatorData.asset - skipping animator assignment");
            }

            AssetDatabase.CreateAsset(warrior, $"{path}/Class_Warrior.asset");
            AssetDatabase.SaveAssets();
            
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = warrior;
            
            Debug.Log("Created Warrior class definition!");
        }

        private void CreateFireSword()
        {
            string path = "Assets/RogueDeal/Resources/Data/Equipment";
            if (!AssetDatabase.IsValidFolder(path))
            {
                System.IO.Directory.CreateDirectory(path);
            }

            var fireSword = ScriptableObject.CreateInstance<EquipmentItem>();
            fireSword.itemId = "weapon_fire_sword_001";
            fireSword.displayName = "Blazing Sword";
            fireSword.description = "A sword wreathed in flames. Has a chance to burn enemies.";
            fireSword.rarity = ItemRarity.Uncommon;
            fireSword.goldValue = 500;
            fireSword.slot = EquipmentSlot.Weapon;
            fireSword.requiredLevel = 5;
            
            fireSword.attackBonus = 15;
            fireSword.damageBonus = 5;
            fireSword.critChanceBonus = 0.05f;
            
            fireSword.elementalType = ElementalType.Fire;

            AssetDatabase.CreateAsset(fireSword, $"{path}/Equipment_FireSword.asset");
            AssetDatabase.SaveAssets();
            
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = fireSword;
            
            Debug.Log("Created Fire Sword equipment! (Note: Assign a StatusEffectDefinition for burning in the inspector)");
        }

        private void CreateTestEnemy()
        {
            string path = "Assets/RogueDeal/Resources/Data/Enemies";
            if (!AssetDatabase.IsValidFolder(path))
            {
                System.IO.Directory.CreateDirectory(path);
            }

            var goblin = ScriptableObject.CreateInstance<EnemyDefinition>();
            goblin.enemyId = "enemy_goblin_001";
            goblin.displayName = "Goblin Raider";
            goblin.description = "A weak but aggressive goblin enemy.";
            
            goblin.baseStats = new CharacterStats
            {
                maxHealth = 50,
                currentHealth = 50,
                attack = 8,
                damage = 5,
                magic = 0,
                defense = 3,
                damageMultiplier = 1f,
                defenseMultiplier = 1f
            };
            
            goblin.statsPerWorldMultiplier = 1.5f;
            goblin.attackDamage = 10;
            goblin.attackType = DamageType.Physical;
            goblin.attackDelay = 1f;
            
            goblin.baseGoldDrop = 15;
            goblin.baseXPReward = 25;

            AssetDatabase.CreateAsset(goblin, $"{path}/Enemy_Goblin.asset");
            AssetDatabase.SaveAssets();
            
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = goblin;
            
            Debug.Log("Created Goblin enemy definition!");
        }
        
        private void CreateTestLevel()
        {
            string path = "Assets/RogueDeal/Resources/Data/Levels";
            if (!AssetDatabase.IsValidFolder(path))
            {
                System.IO.Directory.CreateDirectory(path);
            }

            var goblinEnemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/RogueDeal/Resources/Data/Enemies/Enemy_Goblin.asset");
            if (goblinEnemy == null)
            {
                Debug.LogWarning("Goblin enemy not found! Create the enemy first.");
                CreateTestEnemy();
                goblinEnemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/RogueDeal/Resources/Data/Enemies/Enemy_Goblin.asset");
            }

            var testLevel = ScriptableObject.CreateInstance<LevelDefinition>();
            testLevel.levelId = "level_test_001";
            testLevel.displayName = "Test Combat Arena";
            testLevel.description = "A test level for combat testing with 2 goblins.";
            testLevel.worldNumber = 1;
            testLevel.levelNumber = 1;
            testLevel.requiredPlayerLevel = 1;
            testLevel.energyCost = 0;
            testLevel.totalTurns = 10;
            testLevel.baseGoldReward = 50;
            testLevel.baseXPReward = 100;
            testLevel.twoStarTurnsRemaining = 3;
            testLevel.threeStarTurnsRemaining = 6;
            
            if (goblinEnemy != null)
            {
                testLevel.enemySpawns = new System.Collections.Generic.List<EnemySpawn>
                {
                    new EnemySpawn { enemy = goblinEnemy, positionIndex = 0, isBoss = false },
                    new EnemySpawn { enemy = goblinEnemy, positionIndex = 1, isBoss = false }
                };
            }

            AssetDatabase.CreateAsset(testLevel, $"{path}/Level_Test.asset");
            AssetDatabase.SaveAssets();
            
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = testLevel;
            
            Debug.Log("Created Test Level definition!");
        }
    }
}
