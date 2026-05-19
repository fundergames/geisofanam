/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 */

using UnityEngine;

namespace Geis.Locomotion
{
    /// <summary>
    /// Sets the HitReaction override layer weight when entering a state (0 on empty = passthrough to base locomotion).
    /// </summary>
    public class GeisHitReactionLayerWeightBehaviour : StateMachineBehaviour
    {
        [Range(0f, 1f)]
        public float layerWeight;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            animator.SetLayerWeight(layerIndex, layerWeight);
        }
    }
}
