using System.Collections;
using UnityEngine;
using DG.Tweening;

namespace FunderGames.RPG
{
    [CreateAssetMenu(fileName = "CardWorldMovementStep", menuName = "FunderGames/ActionSteps/CardWorldMovementStep")]
    public class CardWorldMovementStep : ActionStep
    {
        [SerializeField] private float jumpPower = 2f; // Height of each hop
        [SerializeField] private int numJumps = 2; // Number of hops
        [SerializeField] private float duration = 1.0f; // Duration of movement
        [SerializeField] private float approachDistance = 1.5f; // How close to get to the target
        
        public override IEnumerator Execute(Combatant performer, Combatant target)
        {
            // Calculate the approach position (close to target but not inside them)
            Vector3 targetPosition = target.transform.position;
            Vector3 performerPosition = performer.transform.position;
            Vector3 directionToTarget = (targetPosition - performerPosition).normalized;
            Vector3 approachPosition = targetPosition - directionToTarget * approachDistance;
            
            // Use DOJump to create a hopping effect to the approach position
            yield return performer.transform
                .DOJump(approachPosition, jumpPower, numJumps, duration)
                .SetEase(Ease.InOutQuad)
                .WaitForCompletion();
        }
    }
}
