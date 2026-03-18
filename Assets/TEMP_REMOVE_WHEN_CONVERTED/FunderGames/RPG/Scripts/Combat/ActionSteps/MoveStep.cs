using System.Collections;
using System.ComponentModel;
using UnityEngine;
using DG.Tweening;

namespace FunderGames.RPG
{
    [CreateAssetMenu(fileName = "MoveStep", menuName = "FunderGames/ActionSteps/MoveStep")]
    public class MoveStep : ActionStep
    {
        [Description("The total duration of the action.")]
        [SerializeField] protected float duration = 1.0f;
        public override IEnumerator Execute(Combatant performer, Combatant target)
        {
            // Linear movement using DOTween
            yield return performer.transform
                .DOMove(target.transform.position, duration)
                .SetEase(Ease.Linear) // Linear easing for consistent speed
                // .OnComplete(() => onArrival?.Invoke())
                .WaitForCompletion();
        }
    }
}