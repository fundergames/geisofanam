using System.Collections;
using UnityEngine;

namespace FunderGames.RPG
{
    [CreateAssetMenu(fileName = "ProjectileStep", menuName = "FunderGames/ActionSteps/ProjectileStep")]
    public class ProjectileStep : ActionStep
    {
        public GameObject projectilePrefab;

        public override IEnumerator Execute(Combatant performer, Combatant target)
        {
            // Instantiate and launch the projectile
            GameObject projectile =
                Object.Instantiate(projectilePrefab, performer.transform.position, Quaternion.identity);
            Projectile projectileComponent = projectile.GetComponent<Projectile>();
            projectileComponent.Launch(performer, target, performer.Stats.GetStatByType(StatType.Magic).Amount);

            // Wait for the projectile to reach the target
            yield return new WaitUntil(() => projectileComponent.HasHitTarget);
        }
    }
}