using UnityEngine;

namespace FunderGames.RPG
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(CharacterController))]
    public class PlayerAnimationController : MonoBehaviour
    {
        [Header("Animation Data")]
        [SerializeField] private ClassAnimatorData animatorData;
        
        [Header("Animation Parameters")]
#pragma warning disable CS0414
        [SerializeField] private float speedThreshold = 0.1f;
#pragma warning restore CS0414
        [SerializeField] private float runSpeedThreshold = 6f;
        
        private Animator animator;
        private CharacterController characterController;
        private PlayerController playerController;
        
        // Animation parameter hashes for performance
        private readonly int speedHash = Animator.StringToHash("Speed");
        private readonly int isGroundedHash = Animator.StringToHash("IsGrounded");
        private readonly int isRunningHash = Animator.StringToHash("IsRunning");
        private readonly int isAttackingHash = Animator.StringToHash("IsAttacking");
        private readonly int attackHash = Animator.StringToHash("Attack");
        private readonly int takeDamageHash = Animator.StringToHash("TakeDamage");
        private readonly int dieHash = Animator.StringToHash("Die");
        private readonly int jumpHash = Animator.StringToHash("Jump");
        private readonly int isRollingHash = Animator.StringToHash("IsRolling");
        private readonly int isDashingHash = Animator.StringToHash("IsDashing");
        private readonly int isDefendingHash = Animator.StringToHash("IsDefending");
        private readonly int isDizzyHash = Animator.StringToHash("IsDizzy");
        private readonly int isVictoryHash = Animator.StringToHash("IsVictory");
        private readonly int isLevelUpHash = Animator.StringToHash("IsLevelUp");
        private readonly int isTauntingHash = Animator.StringToHash("IsTaunting");
        
        private void Awake()
        {
            animator = GetComponent<Animator>();
            characterController = GetComponent<CharacterController>();
            playerController = GetComponent<PlayerController>();
            
            // Validate animator data
            if (animatorData == null)
            {
                Debug.LogError("NoWeapon_AnimatorData not assigned! Please assign it in the inspector.");
            }
        }
        
        private void Update()
        {
            UpdateMovementAnimations();
            UpdateGroundedState();
        }
        
        private void UpdateMovementAnimations()
        {
            if (characterController == null) return;
            
            Vector3 velocity = characterController.velocity;
            float speed = new Vector3(velocity.x, 0, velocity.z).magnitude;
            
            animator.SetFloat(speedHash, speed);
            animator.SetBool(isRunningHash, speed > runSpeedThreshold);
        }
        
        private void UpdateGroundedState()
        {
            if (characterController == null) return;
            
            bool isGrounded = characterController.isGrounded;
            animator.SetBool(isGroundedHash, isGrounded);
        }
        
        // Public methods for triggering animations
        public void TriggerJump()
        {
            animator.SetTrigger(jumpHash);
        }
        
        public void TriggerAttack(int attackNumber = 1)
        {
            animator.SetTrigger(attackHash);
            animator.SetBool(isAttackingHash, true);
        }
        
        public void StopAttack()
        {
            animator.SetBool(isAttackingHash, false);
        }
        
        public void TriggerTakeDamage(int damageType = 1)
        {
            animator.SetTrigger(takeDamageHash);
        }
        
        public void TriggerDie()
        {
            animator.SetTrigger(dieHash);
        }
        
        public void TriggerRoll()
        {
            animator.SetBool(isRollingHash, true);
        }
        
        public void StopRoll()
        {
            animator.SetBool(isRollingHash, false);
        }
        
        public void TriggerDash()
        {
            animator.SetBool(isDashingHash, true);
        }
        
        public void StopDash()
        {
            animator.SetBool(isDashingHash, false);
        }
        
        public void SetDefending(bool isDefending)
        {
            animator.SetBool(isDefendingHash, isDefending);
        }
        
        public void SetDizzy(bool isDizzy)
        {
            animator.SetBool(isDizzyHash, isDizzy);
        }
        
        public void SetVictory(bool isVictory)
        {
            animator.SetBool(isVictoryHash, isVictory);
        }
        
        public void SetLevelUp(bool isLevelUp)
        {
            animator.SetBool(isLevelUpHash, isLevelUp);
        }
        
        public void SetTaunting(bool isTaunting)
        {
            animator.SetBool(isTauntingHash, isTaunting);
        }
        
        // Animation event callbacks
        public void OnAttackStart()
        {
            // Called from animation events
            if (playerController != null)
            {
                // Notify player controller that attack has started
            }
        }
        
        public void OnAttackEnd()
        {
            // Called from animation events
            StopAttack();
            if (playerController != null)
            {
                // Notify player controller that attack has ended
            }
        }
        
        public void OnRollStart()
        {
            // Called from animation events
        }
        
        public void OnRollEnd()
        {
            // Called from animation events
            StopRoll();
        }
        
        public void OnDashStart()
        {
            // Called from animation events
        }
        
        public void OnDashEnd()
        {
            // Called from animation events
            StopDash();
        }
        
        // Helper methods to get animation clips from the data asset
        public AnimationClip GetIdleClip()
        {
            return animatorData != null ? animatorData.idleClip : null;
        }
        
        public AnimationClip GetBattleIdleClip()
        {
            return animatorData != null ? animatorData.battleIdleClip : null;
        }
        
        public AnimationClip GetSprintClip()
        {
            return animatorData != null ? animatorData.sprintClip : null;
        }
        
        public AnimationClip GetAttackClip(int attackNumber)
        {
            if (animatorData == null) return null;
            
            switch (attackNumber)
            {
                case 1: return animatorData.attack1Clip;
                case 2: return animatorData.attack2Clip;
                case 3: return animatorData.attack3Clip;
                case 4: return animatorData.attack4Clip;
                case 5: return animatorData.attack5Clip;
                default: return animatorData.attack1Clip;
            }
        }
        
        public AnimationClip GetComboClip()
        {
            return animatorData != null ? animatorData.comboClip : null;
        }
        
        public AnimationClip GetTakeDamageClip(int damageType)
        {
            if (animatorData == null) return null;
            
            return damageType == 1 ? animatorData.takeDamage1Clip : animatorData.takeDamage2Clip;
        }
        
        public AnimationClip GetDieClip()
        {
            return animatorData != null ? animatorData.dieClip : null;
        }
        
        public AnimationClip GetDefendClip()
        {
            return animatorData != null ? animatorData.defendClip : null;
        }
        
        public AnimationClip GetDizzyClip()
        {
            return animatorData != null ? animatorData.dizzyClip : null;
        }
        
        public AnimationClip GetVictoryClip()
        {
            return animatorData != null ? animatorData.victoryClip : null;
        }
        
        public AnimationClip GetLevelUpClip()
        {
            return animatorData != null ? animatorData.levelUpClip : null;
        }
        
        public AnimationClip GetTauntClip()
        {
            return animatorData != null ? animatorData.tauntAnimationClip : null;
        }
    }
}
