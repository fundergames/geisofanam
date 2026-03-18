using UnityEditor.Animations;
using UnityEngine;

[CreateAssetMenu(fileName = "AnimatorData", menuName = "FunderGames/Animator Data")]
public class ClassAnimatorData : ScriptableObject
{
    public AnimatorController battleAnimator;
    public AnimatorController characterSelectAnimator;
    
    [Header("Animations")]
    public AnimationClip idleClip;
    public AnimationClip attack1Clip;
    public AnimationClip attack2Clip;
    public AnimationClip attack3Clip;
    public AnimationClip attack4Clip;
    public AnimationClip attack5Clip;
    public AnimationClip tauntAnimationClip;
    public AnimationClip battleIdleClip;
    public AnimationClip levelUpClip;
    public AnimationClip dieClip;
    public AnimationClip dizzyClip;
    public AnimationClip takeDamage1Clip;
    public AnimationClip takeDamage2Clip;
    public AnimationClip defendClip;
    public AnimationClip victoryClip;
    public AnimationClip sprintClip;
    public AnimationClip comboClip;
}
