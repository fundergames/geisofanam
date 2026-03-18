using System.Collections;
using UnityEngine;
using DG.Tweening;

namespace FunderGames.RPG
{
    [CreateAssetMenu(fileName = "ReturnStep", menuName = "FunderGames/ActionSteps/ReturnStep")]
    public class ReturnStep : MoveStep
    {
        [SerializeField] private float jumpPower = 2f; // Height of each hop
        [SerializeField] private int numJumps = 2; // Number of hops
        
        public override IEnumerator Execute(Combatant performer, Combatant target)
        {
            var worldTarget = performer.transform.parent.TransformPoint(Vector3.zero);
            
            // Use DOJump to create a hopping effect.
            yield return performer.transform
                .DOJump(worldTarget, jumpPower, numJumps, duration)
                .SetEase(Ease.InOutQuad)
                // .OnComplete(() => onArrival?.Invoke())
                .WaitForCompletion();
        }
    }
}