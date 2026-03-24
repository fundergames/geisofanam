using RogueDeal.Combat;
using RogueDeal.Combat.StatusEffects;
using UnityEngine;

namespace RogueDeal.Enemies
{
    public class EnemyInstance
    {
        public EnemyDefinition definition;
        public CharacterStats stats;
        public StatusEffectManager statusEffects;
        public int worldLevel;
        public GameObject visualInstance;
        public Vector3 position;
        public bool isDefeated;

        public EnemyInstance(EnemyDefinition definition, int worldLevel, Vector3 position)
        {
            this.definition = definition;
            this.worldLevel = worldLevel;
            this.position = position;
            this.stats = definition.GetScaledStats(worldLevel);
            this.statusEffects = new StatusEffectManager(stats);
            this.isDefeated = false;
        }

        public int TakeDamage(int damage, DamageType damageType, ElementalType element)
        {
            if (isDefeated)
                return 0;

            float resistance = stats.GetResistance(element);
            int finalDamage = Mathf.RoundToInt(damage * (1f - resistance));

            if (HasImmunity(element))
            {
                finalDamage = 0;
            }

            finalDamage = Mathf.Max(0, finalDamage - stats.defense);

            stats.currentHealth -= finalDamage;

            if (stats.currentHealth <= 0)
            {
                stats.currentHealth = 0;
                isDefeated = true;
            }

            return finalDamage;
        }

        public bool HasImmunity(ElementalType element)
        {
            if (definition.immunities == null || definition.immunities.Count == 0)
                return false;

            foreach (var immunity in definition.immunities)
            {
                if (immunity.associatedElement == element)
                    return true;
            }

            return false;
        }

        public int ProcessStatusEffects()
        {
            return statusEffects.ProcessEffectsOnTurnStart();
        }

        public void CleanupVisual()
        {
            if (visualInstance != null)
            {
                Object.Destroy(visualInstance);
            }
        }
    }
}
