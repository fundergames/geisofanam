using Funder.Core.Events;
using RogueDeal.Combat;
using RogueDeal.Crafting;
using RogueDeal.Enemies;
using RogueDeal.Items;

namespace RogueDeal.Events
{
    public struct CombatStartedEvent : IEvent
    {
        public int stageId;
        public int enemyCount;
    }

    public struct PlayerAttackEvent : IEvent
    {
        public int damageDealt;
        public bool isCrit;
        public DamageType damageType;
        public EnemyInstance target;
        public int hitNumber;
        public int totalHits;
    }

    public struct EnemyAttackEvent : IEvent
    {
        public EnemyInstance attacker;
        public int damageDealt;
        public bool dodged;
    }

    public struct EnemyDefeatedEvent : IEvent
    {
        public EnemyInstance enemy;
        public int goldDropped;
        public BaseItem[] itemsDropped;
    }

    public struct CombatEndedEvent : IEvent
    {
        public bool playerVictory;
        public int turnsUsed;
        public int totalTurns;
        public int goldEarned;
        public int xpEarned;
    }

    public struct TurnStartEvent : IEvent
    {
        public int turnNumber;
        public int remainingTurns;
    }

    public struct StatusEffectAppliedEvent : IEvent
    {
        public StatusEffectType effectType;
        public int stacks;
        public bool isPlayer;
    }

    public struct LevelUpEvent : IEvent
    {
        public int newLevel;
        public CharacterStats newStats;
    }

    public struct ItemCraftedEvent : IEvent
    {
        public CraftingRecipe recipe;
        public CraftingResult result;
        public CraftingIngredient ingredientA;
        public CraftingIngredient ingredientB;
        public CraftingIngredient ingredientC;
    }

    public struct EquipmentChangedEvent : IEvent
    {
        public EquipmentSlot slot;
        public EquipmentItem oldItem;
        public EquipmentItem newItem;
    }
}
