using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace RogueDeal.Combat.Presentation
{
    /// <summary>
    /// Dedicated component for handling root motion continuity during Timeline playback.
    /// This separates position tracking concerns from CombatExecutor.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class TimelineRootMotionController : MonoBehaviour
    {
        private Animator animator;
        private PlayableDirector timelineDirector;
        private Vector3 startPosition;
        private Vector3 accumulatedPosition;
        private Vector3 lastValidPosition;
        private Quaternion startRotation;
        private Quaternion accumulatedRotation;
        private bool isActive = false;
        private bool trackRotation = false; // Set to true if animations have root rotation
        
        [Tooltip("If true, root motion will be rotated from animation forward (Z) to character's visual forward (X). DISABLED - animations control movement direction.")]
        public bool rotateRootMotionToCharacterForward = false; // DISABLED - let animations control movement
        
        private void Awake()
        {
            animator = GetComponent<Animator>();
            if (animator != null)
            {
                animator.applyRootMotion = true;
                Debug.Log($"[TimelineRootMotionController] Awake: Animator found, applyRootMotion = {animator.applyRootMotion}");
            }
            else
            {
                Debug.LogError($"[TimelineRootMotionController] Awake: Animator is null!");
            }
        }
        
        /// <summary>
        /// Starts tracking root motion for a Timeline
        /// </summary>
        public void StartTracking(PlayableDirector director)
        {
            timelineDirector = director;
            startPosition = transform.position;
            accumulatedPosition = Vector3.zero;
            lastValidPosition = transform.position;
            startRotation = transform.rotation;
            accumulatedRotation = Quaternion.identity;
            isActive = true;
            
            if (animator != null)
            {
                // CRITICAL: Ensure root motion is enabled
                animator.applyRootMotion = true;
                
                // Check if Animator has a controller (Timeline might override this)
                bool hasController = animator.runtimeAnimatorController != null;
                
                Debug.Log($"[TimelineRootMotionController] Started tracking.");
                Debug.Log($"[TimelineRootMotionController]   Start position (world): {startPosition}");
                Debug.Log($"[TimelineRootMotionController]   Start rotation: {startRotation.eulerAngles}");
                Debug.Log($"[TimelineRootMotionController]   Root motion enabled: {animator.applyRootMotion}");
                Debug.Log($"[TimelineRootMotionController]   Has Animator Controller: {hasController}");
                Debug.Log($"[TimelineRootMotionController]   Timeline Director: {director != null}");
                
                if (!animator.applyRootMotion)
                {
                    Debug.LogError($"[TimelineRootMotionController] WARNING: Root motion is NOT enabled! This will prevent movement.");
                }
            }
            else
            {
                Debug.LogError($"[TimelineRootMotionController] Cannot start tracking - Animator is null!");
            }
        }
        
        /// <summary>
        /// Stops tracking root motion
        /// </summary>
        public void StopTracking()
        {
            // Before stopping, enforce final position one last time
            // Timeline might try to reset position when it stops
            Vector3 expectedPosition = startPosition + accumulatedPosition;
            float distanceFromExpected = Vector3.Distance(transform.position, expectedPosition);
            
            if (distanceFromExpected > 0.001f)
            {
                Debug.LogWarning($"[TimelineRootMotionController] Position reset detected when stopping! Expected: {expectedPosition}, Was: {transform.position}, Restoring...");
                transform.position = expectedPosition;
            }
            
            // Enforce final rotation if tracking rotation
            if (trackRotation)
            {
                Quaternion expectedRotation = startRotation * accumulatedRotation;
                float rotationDifference = Quaternion.Angle(transform.rotation, expectedRotation);
                
                if (rotationDifference > 0.1f)
                {
                    Debug.LogWarning($"[TimelineRootMotionController] Rotation reset detected when stopping! Expected: {expectedRotation.eulerAngles}, Was: {transform.rotation.eulerAngles}, Restoring...");
                    transform.rotation = expectedRotation;
                }
            }
            
            isActive = false;
            
            // Log detailed final state for debugging
            Vector3 netMovement = accumulatedPosition;
            Vector3 finalExpected = startPosition + accumulatedPosition;
            Debug.Log($"[TimelineRootMotionController] Stopped tracking.");
            Debug.Log($"[TimelineRootMotionController]   Start position: {startPosition}");
            Debug.Log($"[TimelineRootMotionController]   Accumulated root motion: {accumulatedPosition}");
            Debug.Log($"[TimelineRootMotionController]   Net movement: {netMovement}");
            Debug.Log($"[TimelineRootMotionController]   Expected final position: {finalExpected}");
            Debug.Log($"[TimelineRootMotionController]   Actual final position: {transform.position}");
            
            if (trackRotation)
            {
                Debug.Log($"[TimelineRootMotionController]   Start rotation: {startRotation.eulerAngles}");
                Debug.Log($"[TimelineRootMotionController]   Accumulated rotation: {Quaternion.Angle(Quaternion.identity, accumulatedRotation):F1}°");
                Debug.Log($"[TimelineRootMotionController]   Expected final rotation: {(startRotation * accumulatedRotation).eulerAngles}");
                Debug.Log($"[TimelineRootMotionController]   Actual final rotation: {transform.rotation.eulerAngles}");
            }
            
            // Warn if accumulated position suggests dash back didn't work
            if (Mathf.Abs(accumulatedPosition.z) > 1.0f)
            {
                Debug.LogWarning($"[TimelineRootMotionController] WARNING: Final accumulated Z is {accumulatedPosition.z:F2}. " +
                               $"If dash back was supposed to return to start, check that the dash back animation has root motion enabled!");
            }
        }
        
        private void OnAnimatorMove()
        {
            if (!isActive || animator == null || !animator.applyRootMotion)
            {
                // Log why we're not processing root motion
                if (!isActive)
                {
                    // Don't spam - only log once
                    return;
                }
                if (animator == null)
                {
                    Debug.LogError($"[TimelineRootMotionController] OnAnimatorMove: Animator is null!");
                    return;
                }
                if (!animator.applyRootMotion)
                {
                    // Only log occasionally to avoid spam
                    if (Time.frameCount % 60 == 0)
                    {
                        Debug.LogWarning($"[TimelineRootMotionController] OnAnimatorMove: Root motion is disabled! Frame {Time.frameCount}");
                    }
                    return;
                }
                return;
            }
            
            // Root motion deltas are ALWAYS in world space
            Vector3 deltaPosition = animator.deltaPosition;
            Quaternion deltaRotation = animator.deltaRotation;
            
            // Debug: Log if we're getting zero deltas when we shouldn't
            if (deltaPosition.magnitude < 0.0001f && Time.frameCount % 30 == 0)
            {
                Debug.Log($"[TimelineRootMotionController] OnAnimatorMove called but deltaPosition is zero. " +
                         $"IsActive: {isActive}, ApplyRootMotion: {animator.applyRootMotion}, " +
                         $"Timeline State: {(timelineDirector != null ? timelineDirector.state.ToString() : "null")}");
            }
            
            // Apply position root motion
            if (deltaPosition.magnitude > 0.0001f)
            {
                // Store position before applying root motion
                Vector3 positionBefore = transform.position;
                
                // CRITICAL: Timeline may not be transforming root motion by character rotation
                // animator.deltaPosition is in world space, but when using Timeline it might represent
                // movement in the animation's original forward direction (world Z) rather than the
                // character's current forward direction.
                // 
                // If the character is rotated but root motion is still in world Z, we need to
                // transform the delta to account for the character's rotation.
                
                Vector3 animationForward = transform.forward; // Character's Z (animation forward in world space)
                Vector3 normalizedDelta = deltaPosition.normalized;
                float alignment = Vector3.Dot(normalizedDelta, animationForward);
                
                Vector3 finalDelta = deltaPosition;
                
                // If root motion doesn't align with character's forward, Timeline isn't transforming it
                // We need to transform it from animation's local space (Z-forward) to world space
                if (Mathf.Abs(alignment) < 0.7f && deltaPosition.magnitude > 0.01f)
                {
                    // Root motion is in animation's local space (Z-forward), not transformed by character rotation
                    // Transform it: convert from local Z direction to world space based on character's rotation
                    // The delta represents movement in local Z, so we transform it by the character's rotation
                    Vector3 localZDirection = Vector3.forward; // Animation's local forward (Z)
                    Vector3 worldZDirection = transform.TransformDirection(localZDirection); // Character's forward in world
                    
                    // The delta magnitude is correct, but direction needs to be transformed
                    // Project the delta onto the character's forward direction
                    float deltaMagnitude = deltaPosition.magnitude;
                    finalDelta = worldZDirection * deltaMagnitude;
                    
                    Debug.Log($"[TimelineRootMotionController] Transforming root motion: Original {deltaPosition} -> Transformed {finalDelta}");
                }
                
                // Apply root motion
                transform.position += finalDelta;
                accumulatedPosition += finalDelta;
                lastValidPosition = transform.position;
                
                // Log significant root motion for debugging
                if (deltaPosition.magnitude > 0.01f)
                {
                    Vector3 characterForward = transform.right; // Character's visual forward (X)
                    // animationForward and normalizedDelta are already declared above
                    float alignmentWithAnimationForward = Vector3.Dot(normalizedDelta, animationForward);
                    float alignmentWithCharacterForward = Vector3.Dot(normalizedDelta, characterForward);
                    
                    Debug.Log($"[TimelineRootMotionController] Root motion (world): {deltaPosition}, Magnitude: {deltaPosition.magnitude:F2}");
                    Debug.Log($"[TimelineRootMotionController] Character forward (X): {characterForward}, Animation forward (Z): {animationForward}");
                    Debug.Log($"[TimelineRootMotionController] Root motion alignment - Animation forward: {alignmentWithAnimationForward:F2}, Character forward: {alignmentWithCharacterForward:F2}");
                    Debug.Log($"[TimelineRootMotionController] Accumulated: {accumulatedPosition}, Position: {positionBefore} -> {transform.position}");
                    
                    // Warn if root motion doesn't align with animation forward (should be ~1.0 if working correctly)
                    if (Mathf.Abs(alignmentWithAnimationForward) < 0.5f)
                    {
                        Debug.LogWarning($"[TimelineRootMotionController] WARNING: Root motion direction doesn't align with animation forward! " +
                                       $"This suggests Timeline might not be applying root motion correctly. " +
                                       $"Check Timeline Animation Track → 'Apply Transform Offsets' → Rotation should be ENABLED if animations have rotation, or DISABLED if not.");
                    }
                }
            }
            else if (deltaPosition.magnitude > 0.00001f)
            {
                // Even tiny deltas should be accumulated (for precision)
                accumulatedPosition += deltaPosition;
            }
            
            // Apply rotation root motion (if enabled)
            if (trackRotation)
            {
                float rotationAngle = Quaternion.Angle(Quaternion.identity, deltaRotation);
                if (rotationAngle > 0.001f) // Check if rotation is significant (> 0.001 degrees)
                {
                    // Apply rotation root motion
                    transform.rotation = transform.rotation * deltaRotation;
                    accumulatedRotation = accumulatedRotation * deltaRotation;
                    
                    // Log significant rotation for debugging
                    if (rotationAngle > 1.0f)
                    {
                        Debug.Log($"[TimelineRootMotionController] Root rotation: {rotationAngle:F1}°, Accumulated: {Quaternion.Angle(Quaternion.identity, accumulatedRotation):F1}°");
                    }
                }
            }
        }
        
        // Update() removed - not needed with correct Timeline settings
        // Root motion is handled entirely by OnAnimatorMove()
        
        private void LateUpdate()
        {
            if (!isActive || timelineDirector == null) return;
            
            // Safety net: Only check during Timeline playback
            // With correct Timeline settings (Position disabled), this should rarely trigger
            if (timelineDirector.state != PlayState.Playing) return;
            
            // Safety check: If Timeline somehow resets position, restore it
            // This should be rare now that Timeline position control is disabled
            Vector3 expectedPosition = startPosition + accumulatedPosition;
            float distanceFromExpected = Vector3.Distance(transform.position, expectedPosition);
            
            // Only restore if there's a significant deviation (5cm threshold)
            // This prevents false positives from floating point precision issues
            if (distanceFromExpected > 0.05f)
            {
                // Timeline reset detected - restore to accumulated position
                Vector3 positionBefore = transform.position;
                transform.position = expectedPosition;
                lastValidPosition = expectedPosition;
                
                Debug.LogWarning($"[TimelineRootMotionController] Position reset detected at Timeline time {timelineDirector.time:F2}s! Expected: {expectedPosition}, Was: {positionBefore}, Restored. " +
                               $"If this happens frequently, check Timeline Animation Track settings.");
            }
            else
            {
                // Position is valid, update tracking
                lastValidPosition = transform.position;
            }
            
            // Safety check for rotation if tracking rotation
            if (trackRotation)
            {
                Quaternion expectedRotation = startRotation * accumulatedRotation;
                float rotationDifference = Quaternion.Angle(transform.rotation, expectedRotation);
                
                if (rotationDifference > 1.0f)
                {
                    Quaternion rotationBefore = transform.rotation;
                    transform.rotation = expectedRotation;
                    
                    Debug.LogWarning($"[TimelineRootMotionController] Rotation reset detected at Timeline time {timelineDirector.time:F2}s! Expected: {expectedRotation.eulerAngles}, Was: {rotationBefore.eulerAngles}, Restored. " +
                                   $"If this happens frequently, disable Rotation in Timeline Animation Track settings.");
                }
            }
        }
        
        /// <summary>
        /// Gets the accumulated root motion position
        /// </summary>
        public Vector3 GetAccumulatedPosition() => accumulatedPosition;
        
        /// <summary>
        /// Gets the last valid position
        /// </summary>
        public Vector3 GetLastValidPosition() => lastValidPosition;
    }
}

