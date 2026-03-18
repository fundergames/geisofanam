using System.Collections;
using UnityEngine;

namespace RogueDeal.Combat.States
{
    public class CombatIntroState : CombatState
    {
        private bool introComplete = false;

        public CombatIntroState(CombatFlowStateMachine context) : base(context) { }

        public override void OnEnter()
        {
            Debug.Log("[FSM] Entering CombatIntroState");
            introComplete = false;
            context.StartCoroutine(PlayIntro());
        }

        public override void OnTick(float dt)
        {
            if (introComplete)
            {
                Debug.Log("[FSM] Intro complete, transitioning to DealingCardsState");
                context.StateMachine.TryGo<DealingCardsState>();
            }
        }

        private IEnumerator PlayIntro()
        {
            if (context.IntroController != null)
            {
                yield return context.IntroController.PlayIntro();
            }
            else
            {
                Debug.LogWarning("[CombatIntroState] IntroController not set!");
            }

            if (context.CombatManager != null)
            {
                Debug.Log("[CombatIntroState] Requesting initial hand from CombatManager...");
                context.CombatManager.DealNewHand();
            }

            introComplete = true;
        }
    }
}
