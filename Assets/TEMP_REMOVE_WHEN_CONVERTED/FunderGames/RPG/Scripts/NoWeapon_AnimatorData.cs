using UnityEngine;

namespace FunderGames.RPG
{
    [CreateAssetMenu(fileName = "NoWeapon_AnimatorData", menuName = "RPG/NoWeapon Animator Data")]
    public class NoWeapon_AnimatorData : ScriptableObject
    {
        [Header("Animator Controllers")]
        public RuntimeAnimatorController battleAnimator;
        public RuntimeAnimatorController characterSelectAnimator;
        
        [Header("Basic Animations")]
        public AnimationClip idleClip;
        public AnimationClip battleIdleClip;
        public AnimationClip sprintClip;
        
        [Header("Attack Animations")]
        public AnimationClip attack1Clip;
        public AnimationClip attack2Clip;
        public AnimationClip attack3Clip;
        public AnimationClip attack4Clip;
        public AnimationClip attack5Clip;
        public AnimationClip comboClip;
        
        [Header("Status Animations")]
        public AnimationClip takeDamage1Clip;
        public AnimationClip takeDamage2Clip;
        public AnimationClip dieClip;
        public AnimationClip dizzyClip;
        public AnimationClip defendClip;
        public AnimationClip victoryClip;
        public AnimationClip levelUpClip;
        public AnimationClip tauntAnimationClip;
        
        [Header("Movement Animations")]
        public AnimationClip walkClip;
        public AnimationClip runClip;
        public AnimationClip jumpClip;
        public AnimationClip rollClip;
        public AnimationClip dashClip;
        
        [Header("Interaction Animations")]
        public AnimationClip carryStartClip;
        public AnimationClip carryMoveIdleClip;
        public AnimationClip carryThrowClip;
        public AnimationClip interactGateClip;
        public AnimationClip interactPeopleClip;
        
        [Header("Social Animations")]
        public AnimationClip greeting1Clip;
        public AnimationClip greeting2Clip;
        public AnimationClip danceClip;
        public AnimationClip challengingClip;
        
        [Header("Special Animations")]
        public AnimationClip senseStartClip;
        public AnimationClip senseSearchingClip;
        public AnimationClip foundSomethingClip;
        public AnimationClip drinkPotionClip;
        public AnimationClip getUpClip;
        public AnimationClip sleepingClip;
        
        [Header("Root Motion Animations")]
        public AnimationClip moveFWDNormalClip;
        public AnimationClip moveFWDBattleClip;
        public AnimationClip moveBWDBattleClip;
        public AnimationClip moveLFTBattleClip;
        public AnimationClip moveRGTBattleClip;
        public AnimationClip rollFWDBattleClip;
        public AnimationClip rollBWDBattleClip;
        public AnimationClip rollLFTBattleClip;
        public AnimationClip rollRGTBattleClip;
        public AnimationClip dashFWDBattleClip;
        public AnimationClip dashBWDBattleClip;
        public AnimationClip dashLFTBattleClip;
        public AnimationClip dashRGTBattleClip;
        public AnimationClip jumpFullClip;
        public AnimationClip jumpFullSpinClip;
        public AnimationClip swimmingClip;
        public AnimationClip swimmingFloatingClip;
        public AnimationClip climbUpClip;
        public AnimationClip climbDownClip;
        public AnimationClip pushClip;
        
        // Helper methods to get animation clips
        public AnimationClip GetAttackClip(int attackNumber)
        {
            switch (attackNumber)
            {
                case 1: return attack1Clip;
                case 2: return attack2Clip;
                case 3: return attack3Clip;
                case 4: return attack4Clip;
                case 5: return attack5Clip;
                default: return attack1Clip;
            }
        }
        
        public AnimationClip GetTakeDamageClip(int damageType)
        {
            return damageType == 1 ? takeDamage1Clip : takeDamage2Clip;
        }
        
        public AnimationClip GetGreetingClip(int greetingNumber)
        {
            return greetingNumber == 1 ? greeting1Clip : greeting2Clip;
        }
        
        public AnimationClip GetMoveClip(string direction, bool isBattle = false)
        {
            switch (direction.ToUpper())
            {
                case "FWD":
                    return isBattle ? moveFWDBattleClip : moveFWDNormalClip;
                case "BWD":
                    return moveBWDBattleClip;
                case "LFT":
                    return moveLFTBattleClip;
                case "RGT":
                    return moveRGTBattleClip;
                default:
                    return moveFWDNormalClip;
            }
        }
        
        public AnimationClip GetRollClip(string direction)
        {
            switch (direction.ToUpper())
            {
                case "FWD": return rollFWDBattleClip;
                case "BWD": return rollBWDBattleClip;
                case "LFT": return rollLFTBattleClip;
                case "RGT": return rollRGTBattleClip;
                default: return rollFWDBattleClip;
            }
        }
        
        public AnimationClip GetDashClip(string direction)
        {
            switch (direction.ToUpper())
            {
                case "FWD": return dashFWDBattleClip;
                case "BWD": return dashBWDBattleClip;
                case "LFT": return dashLFTBattleClip;
                case "RGT": return dashRGTBattleClip;
                default: return dashFWDBattleClip;
            }
        }
    }
}
