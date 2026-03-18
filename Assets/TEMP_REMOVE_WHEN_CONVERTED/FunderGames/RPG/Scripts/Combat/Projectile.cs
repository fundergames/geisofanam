using System.Collections;
using UnityEngine;

namespace FunderGames.RPG
{
    public class Projectile : MonoBehaviour
    {
        public int Damage { get; private set; }
        public bool HasHitTarget { get; private set; } // This property tracks whether the projectile has hit the target

        private Combatant target;
        private float speed = 10f;

        // Method to launch the projectile toward a target
        public void Launch(Combatant attacker, Combatant target, int damageAmount)
        {
            this.target = target;
            Damage = damageAmount;
            HasHitTarget = false; // Set it to false at the start of the launch
            StartCoroutine(MoveToTarget());
        }

        // Coroutine to move the projectile towards the target
        private IEnumerator MoveToTarget()
        {
            while (Vector3.Distance(transform.position, target.transform.position) > 0.1f)
            {
                transform.position =
                    Vector3.MoveTowards(transform.position, target.transform.position, speed * Time.deltaTime);
                yield return null;
            }

            // Once the projectile reaches the target, mark it as hit and apply damage
            HasHitTarget = true;
            target.TakeDamage(Damage);

            // Optionally, play a hit animation or effect on the target here
            target.PlayAnimation("TakeDamage");

            // Destroy the projectile after it hits
            Destroy(gameObject);
        }
    }
}