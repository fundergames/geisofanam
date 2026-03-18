using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace FunderGames.RPG
{
    public class Combatant : MonoBehaviour
    {
        public Animator animator;
        private Vector3 originalPosition;
        [SerializeField] private Transform contentTransform;
        public List<CombatAction> AvailableActions { get; } = new();

        public string Name { get; private set; }
        public int Health { get; private set; }
        public int AttackPower { get; private set; }
        public int Mana { get; private set; }

        public StatsData Stats { get; private set; }
        public CharacterClassData CharacterClass { get; private set; }
        public GameObject CharacterModel { get; private set; }

        // Method to initialize Combatant from HeroData (same as before)
        public void InitializeFromHeroData(HeroData heroData, int stencilId = 1)
        {
            originalPosition = contentTransform.position;
            Name = heroData.PlayerName;
            Health = heroData.StatList.GetStatByType(StatType.Health).Amount;
            AttackPower = heroData.StatList.GetStatByType(StatType.Attack).Amount;
            Mana = heroData.StatList.GetStatByType(StatType.Mana).Amount;

            Stats = heroData.StatList;
            CharacterClass = heroData.CharacterClass;

            if (heroData.HeroVisualData.characterPrefab != null)
            {
                CharacterModel = Instantiate(heroData.HeroVisualData.characterPrefab, contentTransform);
                CharacterModel.transform.localScale = Vector3.one * 2.5f;
                CharacterModel.transform.Rotate(Vector3.up, 180f);
                var anim = CharacterModel.GetComponent<Animator>();
                if (anim != null)
                    animator = anim;

                if (heroData.AnimatorData != null && heroData.AnimatorData.battleAnimator != null)
                {
                    var animController = new AnimatorOverrideController(heroData.AnimatorData.battleAnimator)
                        {
                            ["Idle_Battle_NoWeapon"] = heroData.AnimatorData.battleIdleClip,
                            ["Attack01_NoWeapon"] = heroData.AnimatorData.attack1Clip,
                            ["Attack02_NoWeapon"] = heroData.AnimatorData.attack2Clip
                        };
                    animator.runtimeAnimatorController = animController;
                }
            }

            foreach(var action in heroData.AvailableActions)
            {
                AvailableActions.Add(action);
            }
        }

        // Coroutine to perform an action and wait for it to complete
        public IEnumerator PerformAction(CombatAction action, Combatant target)
        {
            yield return action.Execute(this, target);
        }

        // Move back to the original position after the action
        public IEnumerator MoveBackToOriginalPosition()
        {
            var moveBack = ScriptableObject.CreateInstance<MoveStep>();
            yield return StartCoroutine(MoveToPosition(originalPosition, null));
        }

        private IEnumerator MoveToPosition(Vector3 targetPosition, System.Action onArrival)
        {
            float jumpPower = 2f; // Height of each hop
            int numJumps = 2; // Number of hops
            float duration = 1.5f; // Total duration of the movement
            
            // Use DOJump to create a hopping effect.
            yield return transform
                .DOJump(targetPosition, jumpPower, numJumps, duration)
                .SetEase(Ease.InOutQuad)
                .OnComplete(() => onArrival?.Invoke())
                .WaitForCompletion();
        }
        
        public void TakeDamage(int damage)
        {
            Health -= Mathf.Max(0, damage);  // Ensure no negative damage
            if (Health <= 0)
            {
                Health = 0;
                Debug.Log($"{Name} has been defeated.");
            }
        }
        
        public void Heal(int amount)
        {
            Health += amount; 
        }

        public void AdjustMana(int amount)
        {
            Mana += amount;
        }
        
        public void PlayAnimation(string animationName)
        {
            // animator.Play(animationName);
            animator.SetTrigger(animationName);
        }
    }
}
