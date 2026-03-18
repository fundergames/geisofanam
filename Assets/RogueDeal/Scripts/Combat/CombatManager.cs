using Funder.Core.Events;
using Funder.Core.Randoms;
using RogueDeal.Combat.Cards;
using RogueDeal.Enemies;
using RogueDeal.Events;
using RogueDeal.Levels;
using RogueDeal.Player;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RogueDeal.Combat
{
    public class CombatManager
    {
        private readonly IRandomHub _randomHub;
        private PlayerCharacter player;
        private LevelDefinition currentLevel;
        private List<EnemyInstance> enemies = new List<EnemyInstance>();
        private int currentEnemyIndex = 0;
        private int currentTurn = 0;
        private bool isRoyalFlushMode = false;
        private float combatStartTime = 0f;
        
        private Deck deck;
        private List<Card> currentHand = new List<Card>();
        private List<bool> heldCards = new List<bool> { false, false, false, false, false };
        
        private PokerHandType? lastHandType;
        private DamageResult? lastDamageResult;
        private PokerHandDefinition lastHandDefinition;
        private ClassAttackMapping lastAttackMapping;
        private DamageType lastDamageType;
        
        private int currentHitIndex = 0;
        private int totalHitsInCombo = 0;

        public PlayerCharacter Player => player;
        public LevelDefinition CurrentLevel => currentLevel;
        public EnemyInstance CurrentEnemy => currentEnemyIndex < enemies.Count ? enemies[currentEnemyIndex] : null;
        public List<Card> CurrentHand => currentHand;
        public int CurrentTurn => currentTurn;
        public int RemainingTurns => currentLevel.totalTurns - currentTurn;
        public bool IsRoyalFlushMode => isRoyalFlushMode;

        public CombatManager(IRandomHub randomHub)
        {
            _randomHub = randomHub ?? throw new System.ArgumentNullException(nameof(randomHub));
        }

        public void StartCombat(PlayerCharacter player, LevelDefinition level)
        {
            this.player = player;
            this.currentLevel = level;
            this.currentTurn = 0;
            this.currentEnemyIndex = 0;
            this.isRoyalFlushMode = false;
            this.combatStartTime = Time.time;
            
            deck = new Deck(_randomHub, $"Combat_{level.worldNumber}_{level.levelNumber}");
            SpawnEnemies();
            
            EventBus<CombatStartedEvent>.Raise(new CombatStartedEvent
            {
                stageId = level.worldNumber * 100 + level.levelNumber,
                enemyCount = enemies.Count
            });
        }

        private void SpawnEnemies()
        {
            enemies.Clear();
            
            Debug.Log($"[CombatManager] SpawnEnemies - Level: {currentLevel?.displayName ?? "NULL"}");
            
            if (currentLevel == null)
            {
                Debug.LogError("[CombatManager] Cannot spawn enemies - currentLevel is null!");
                return;
            }
            
            if (currentLevel.enemySpawns == null || currentLevel.enemySpawns.Count == 0)
            {
                Debug.LogError($"[CombatManager] Level '{currentLevel.displayName}' has no enemy spawns!");
                return;
            }
            
            Debug.Log($"[CombatManager] Spawning {currentLevel.enemySpawns.Count} enemies...");
            
            foreach (var spawn in currentLevel.enemySpawns)
            {
                if (spawn.enemy == null)
                {
                    Debug.LogError("[CombatManager] Spawn has null enemy definition!");
                    continue;
                }
                
                Vector3 position = spawn.positionIndex < currentLevel.enemyPositions.Length 
                    ? currentLevel.enemyPositions[spawn.positionIndex] 
                    : Vector3.zero;
                
                Debug.Log($"[CombatManager] Creating enemy: {spawn.enemy.displayName} at position {position}");
                
                var enemy = new EnemyInstance(spawn.enemy, currentLevel.worldNumber, position);
                enemies.Add(enemy);
            }
            
            Debug.Log($"[CombatManager] ✅ Created {enemies.Count} enemy instances");
        }

        public void DealNewHand()
        {
            currentHand = deck.DrawHand();
            heldCards = new List<bool> { false, false, false, false, false };
            
            currentTurn++;
            
            EventBus<HandDealtEvent>.Raise(new HandDealtEvent
            {
                cards = currentHand,
                turnNumber = currentTurn
            });
            
            EventBus<TurnStartEvent>.Raise(new TurnStartEvent
            {
                turnNumber = currentTurn,
                remainingTurns = RemainingTurns
            });
        }

        public void ToggleCardHold(int cardIndex)
        {
            if (cardIndex >= 0 && cardIndex < heldCards.Count)
            {
                heldCards[cardIndex] = !heldCards[cardIndex];
            }
        }

        public void DrawCards()
        {
            DrawCards(heldCards);
        }

        public void DrawCards(List<bool> holdFlags)
        {
            if (holdFlags != null && holdFlags.Count == currentHand.Count)
            {
                heldCards = holdFlags;
            }
            
            for (int i = 0; i < currentHand.Count; i++)
            {
                if (!heldCards[i])
                {
                    currentHand[i] = deck.DrawRandomCard();
                }
            }
        }

        public void EvaluateAndAttack()
        {
            EvaluateCurrentHand();
            PerformAttacks();
        }

        public void EvaluateCurrentHand()
        {
            PokerHandType handType = PokerHandEvaluator.EvaluateHand(currentHand);
            
            if (handType == PokerHandType.RoyalFlush && !isRoyalFlushMode)
            {
                isRoyalFlushMode = true;
            }

            var attackMapping = player.classDefinition.GetAttackMapping(handType);
            DamageType damageType = attackMapping?.damageType ?? DamageType.Physical;
            
            var handDef = GetHandDefinition(handType);
            var damageResult = DamageCalculator.CalculateDamage(_randomHub, player, handDef, attackMapping, damageType);
            
            lastHandType = handType;
            lastDamageResult = damageResult;
            lastHandDefinition = handDef;
            lastAttackMapping = attackMapping;
            lastDamageType = damageType;
            
            EventBus<HandEvaluatedEvent>.Raise(new HandEvaluatedEvent
            {
                handType = handType,
                baseDamage = damageResult.baseDamage,
                isCrit = damageResult.isCrit
            });
        }

        public void PerformAttacks()
        {
            if (!lastHandType.HasValue || !lastDamageResult.HasValue)
                return;
                
            PerformPlayerAttackOnly(lastHandType.Value, lastDamageResult.Value);
        }

        public void InitializeAttackCombo()
        {
            if (!lastHandType.HasValue)
            {
                currentHitIndex = 0;
                totalHitsInCombo = 0;
                return;
            }

            currentHitIndex = 0;
            totalHitsInCombo = PokerHandAttackInfo.GetNumberOfHits(lastHandType.Value);
            Debug.Log($"[CombatManager] Initialized attack combo: {totalHitsInCombo} hits");
        }

        public bool PerformNextHit()
        {
            if (currentHitIndex >= totalHitsInCombo)
            {
                Debug.Log("[CombatManager] No more hits to perform");
                return false;
            }

            if (CurrentEnemy == null || CurrentEnemy.isDefeated)
            {
                if (isRoyalFlushMode)
                {
                    Debug.Log("[CombatManager] Enemy defeated in Royal Flush mode, progressing to next enemy");
                    ProgressToNextEnemy();
                    
                    if (CurrentEnemy == null)
                    {
                        Debug.Log("[CombatManager] No more enemies, ending combo");
                        return false;
                    }
                }
                else
                {
                    Debug.Log("[CombatManager] Enemy defeated, ending combo");
                    return false;
                }
            }

            DamageResult hitDamage = DamageCalculator.CalculateDamage(
                _randomHub, 
                player, 
                lastHandDefinition, 
                lastAttackMapping, 
                lastDamageType
            );
            
            Debug.Log($"[Multi-Hit {currentHitIndex + 1}/{totalHitsInCombo}] Base: {hitDamage.baseDamage}, Stat Mod: {hitDamage.statModifier}, Mult: {hitDamage.totalMultiplier:F2}x, Final: {hitDamage.damage}, Crit: {hitDamage.isCrit}");
                
            ElementalType element = GetAttackElement();
            int finalDamage = CurrentEnemy.TakeDamage(hitDamage.damage, hitDamage.damageType, element);
            
            EventBus<PlayerAttackEvent>.Raise(new PlayerAttackEvent
            {
                handType = lastHandType.Value,
                damageDealt = finalDamage,
                isCrit = hitDamage.isCrit,
                damageType = hitDamage.damageType,
                target = CurrentEnemy,
                hitNumber = currentHitIndex + 1,
                totalHits = totalHitsInCombo
            });

            ApplyStatusEffects(CurrentEnemy);
            
            if (CurrentEnemy.isDefeated)
            {
                HandleEnemyDefeat();
            }

            currentHitIndex++;
            return currentHitIndex < totalHitsInCombo || (isRoyalFlushMode && CurrentEnemy != null);
        }

        public int GetTotalHitsInCombo() => totalHitsInCombo;
        public int GetCurrentHitIndex() => currentHitIndex;


        private void PerformPlayerAttackOnly(PokerHandType handType, DamageResult damageResult)
        {
            if (CurrentEnemy == null || CurrentEnemy.isDefeated)
            {
                ProgressToNextEnemy();
                return;
            }

            int numberOfHits = PokerHandAttackInfo.GetNumberOfHits(handType);
            
            for (int hit = 0; hit < numberOfHits; hit++)
            {
                if (CurrentEnemy == null || CurrentEnemy.isDefeated)
                    break;
                
                DamageResult hitDamage = DamageCalculator.CalculateDamage(
                    _randomHub, 
                    player, 
                    lastHandDefinition, 
                    lastAttackMapping, 
                    lastDamageType
                );
                
                Debug.Log($"[Multi-Hit {hit + 1}/{numberOfHits}] Base: {hitDamage.baseDamage}, Stat Mod: {hitDamage.statModifier}, Mult: {hitDamage.totalMultiplier:F2}x, Final: {hitDamage.damage}, Crit: {hitDamage.isCrit}");
                    
                ElementalType element = GetAttackElement();
                int finalDamage = CurrentEnemy.TakeDamage(hitDamage.damage, hitDamage.damageType, element);
                
                EventBus<PlayerAttackEvent>.Raise(new PlayerAttackEvent
                {
                    handType = handType,
                    damageDealt = finalDamage,
                    isCrit = hitDamage.isCrit,
                    damageType = hitDamage.damageType,
                    target = CurrentEnemy,
                    hitNumber = hit + 1,
                    totalHits = numberOfHits
                });

                ApplyStatusEffects(CurrentEnemy);
                
                if (CurrentEnemy.isDefeated)
                {
                    HandleEnemyDefeat();
                    break;
                }
            }

            if (isRoyalFlushMode && CurrentEnemy != null && CurrentEnemy.isDefeated)
            {
                ProgressToNextEnemy();
                if (CurrentEnemy != null)
                {
                    PerformPlayerAttackOnly(handType, damageResult);
                }
                else
                {
                    EndCombat(true);
                    return;
                }
            }

            if (!player.IsAlive())
            {
                EndCombat(false);
                return;
            }

            if (AllEnemiesDefeated())
            {
                EndCombat(true);
                return;
            }

            if (currentTurn >= currentLevel.totalTurns)
            {
                EndCombat(false);
                return;
            }
        }

        private void ApplyStatusEffects(EnemyInstance enemy)
        {
            var weapon = player.equipment.ContainsKey(EquipmentSlot.Weapon) 
                ? player.equipment[EquipmentSlot.Weapon] 
                : null;

            if (weapon?.onHitEffect != null)
            {
                var stream = _randomHub.GetStream("Combat/StatusEffects");
                if (stream.Chance(weapon.onHitEffect.applicationChance))
                {
                    var effect = weapon.onHitEffect.CreateEffect();
                    enemy.statusEffects.AddEffect(effect);
                    
                    EventBus<StatusEffectAppliedEvent>.Raise(new StatusEffectAppliedEvent
                    {
                        effectType = effect.type,
                        stacks = effect.stacks,
                        isPlayer = false
                    });
                }
            }
        }

        public void PerformEnemyAttack()
        {
            if (CurrentEnemy == null || CurrentEnemy.isDefeated)
                return;

            int statusDamage = CurrentEnemy.ProcessStatusEffects();
            if (statusDamage > 0)
            {
                CurrentEnemy.TakeDamage(statusDamage, DamageType.Magic, ElementalType.None);
                if (CurrentEnemy.isDefeated)
                {
                    HandleEnemyDefeat();
                    return;
                }
            }

            int attackDamage = CurrentEnemy.definition.GetScaledAttackDamage(currentLevel.worldNumber);
            int damageTaken = player.TakeDamage(attackDamage);
            bool dodged = damageTaken == 0 && attackDamage > 0;
            
            EventBus<EnemyAttackEvent>.Raise(new EnemyAttackEvent
            {
                attacker = CurrentEnemy,
                damageDealt = damageTaken,
                dodged = dodged
            });
        }

        private void HandleEnemyDefeat()
        {
            int gold = CurrentEnemy.definition.GetScaledGold(currentLevel.worldNumber);
            var loot = CurrentEnemy.definition.lootTable?.RollLoot(_randomHub) ?? new List<Items.BaseItem>();
            
            player.inventory.AddGold(gold);
            foreach (var item in loot)
            {
                player.inventory.AddItem(item);
            }
            
            EventBus<EnemyDefeatedEvent>.Raise(new EnemyDefeatedEvent
            {
                enemy = CurrentEnemy,
                goldDropped = gold,
                itemsDropped = loot.ToArray()
            });

            CheckOnKillAbilities();
        }

        private void CheckOnKillAbilities()
        {
            var abilities = player.classDefinition.GetAvailableAbilities(player.level);
            foreach (var ability in abilities)
            {
                if (ability.triggersOnKill && ability.healOnKillPercent > 0)
                {
                    int healAmount = Mathf.RoundToInt(player.effectiveStats.maxHealth * (ability.healOnKillPercent / 100f));
                    player.Heal(healAmount);
                }
            }
        }

        public void ProgressToNextEnemy()
        {
            currentEnemyIndex++;
            Debug.Log($"[CombatManager] Progressed to next enemy. Index: {currentEnemyIndex}, CurrentEnemy: {(CurrentEnemy != null ? CurrentEnemy.definition.displayName : "NULL")}");
        }

        private bool AllEnemiesDefeated()
        {
            return enemies.All(e => e.isDefeated);
        }

        private void EndCombat(bool victory)
        {
            float combatDuration = Time.time - combatStartTime;
            
            CombatResults results = new CombatResults(currentLevel, victory, currentTurn, combatDuration);
            
            if (victory)
            {
                foreach (var enemy in enemies.Where(e => e.isDefeated))
                {
                    results.XPEarned += enemy.definition.GetScaledXP(currentLevel.worldNumber);
                }

                player.inventory.AddGold(results.GoldEarned);
                player.AddXP(results.XPEarned);
                
                results.SaveToLevelManager();
            }

            EventBus<CombatEndedEvent>.Raise(new CombatEndedEvent
            {
                playerVictory = victory,
                turnsUsed = currentTurn,
                totalTurns = currentLevel.totalTurns,
                goldEarned = results.GoldEarned,
                xpEarned = results.XPEarned
            });

            Debug.Log($"[CombatManager] {results}");
            
            Cleanup();
        }

        private void Cleanup()
        {
            foreach (var enemy in enemies)
            {
                enemy.CleanupVisual();
            }
            enemies.Clear();
        }

        private ElementalType GetAttackElement()
        {
            var weapon = player.equipment.ContainsKey(EquipmentSlot.Weapon) 
                ? player.equipment[EquipmentSlot.Weapon] 
                : null;

            return weapon?.elementalType ?? ElementalType.None;
        }

        private PokerHandDefinition GetHandDefinition(PokerHandType handType)
        {
            var handDef = ScriptableObject.CreateInstance<PokerHandDefinition>();
            handDef.handType = handType;
            handDef.damageRange = GetDefaultDamageRange(handType);
            return handDef;
        }

        private DamageRange GetDefaultDamageRange(PokerHandType handType)
        {
            return handType switch
            {
                PokerHandType.HighCard => new DamageRange(1, 4, 5, 0.1f),
                PokerHandType.Pair => new DamageRange(6, 8, 10, 0.12f),
                PokerHandType.TwoPair => new DamageRange(11, 14, 17, 0.15f),
                PokerHandType.ThreeOfAKind => new DamageRange(18, 22, 27, 0.18f),
                PokerHandType.Straight => new DamageRange(28, 35, 42, 0.2f),
                PokerHandType.Flush => new DamageRange(43, 52, 62, 0.22f),
                PokerHandType.FullHouse => new DamageRange(63, 75, 90, 0.25f),
                PokerHandType.FourOfAKind => new DamageRange(91, 110, 135, 0.28f),
                PokerHandType.StraightFlush => new DamageRange(136, 170, 210, 0.3f),
                PokerHandType.RoyalFlush => new DamageRange(300, 400, 500, 0.35f),
                _ => new DamageRange(1, 4, 5, 0.1f)
            };
        }
    }
}
