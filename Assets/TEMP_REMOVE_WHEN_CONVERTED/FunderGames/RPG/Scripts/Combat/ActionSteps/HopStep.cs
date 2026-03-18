using System.Collections;
using UnityEngine;
using DG.Tweening;

namespace FunderGames.RPG
{
    [CreateAssetMenu(fileName = "HopStep", menuName = "FunderGames/ActionSteps/HopStep")]
    public class HopStep : MoveStep
    {
        [SerializeField] private float jumpPower = 2f; // Height of each hop
        [SerializeField] private int numJumps = 2; // Number of hops
        
        public override IEnumerator Execute(Combatant performer, Combatant target)
        {
            // Use DOJump to create a hopping effect.
            yield return performer.transform
                .DOJump(target.transform.position, jumpPower, numJumps, duration)
                .SetEase(Ease.InOutQuad)
                // .OnComplete(() => onArrival?.Invoke())
                .WaitForCompletion();
        }
    }
}