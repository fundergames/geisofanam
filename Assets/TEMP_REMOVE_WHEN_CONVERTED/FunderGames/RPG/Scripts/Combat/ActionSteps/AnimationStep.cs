using System.Collections;
using UnityEngine;

namespace FunderGames.RPG
{
    [CreateAssetMenu(fileName = "AnimationStep", menuName = "FunderGames/ActionSteps/AnimationStep")]
    public class AnimationStep : ActionStep
    {
        public string animationName; // Name of the animation to play

        public override IEnumerator Execute(Combatant performer, Combatant target)
        {
            // Play the specified animation
            performer.PlayAnimation(animationName);
            yield return new WaitForSeconds(1.0f); // Adjust time to match animation length
        }
    }
}