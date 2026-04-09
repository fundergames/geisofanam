using UnityEngine;

namespace Geis.SoulRealm
{
    /// <summary>
    /// Attach to physical-world actors that should stop simulating while soul realm is active (selective freeze; no Time.timeScale).
    /// </summary>
    public sealed class SoulRealmFreezeTarget : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Rigidbody rigidBody;
        [SerializeField] private bool kinematicWhenFrozen = true;
        [SerializeField] private bool pauseParticlesWhenFrozen = true;

        private float _savedAnimatorSpeed = 1f;
        private bool _frozen;
        private ParticleSystem[] _particles;
        private bool[] _particleWasPlaying;

        private void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>();
            if (rigidBody == null)
                rigidBody = GetComponent<Rigidbody>();

            if (pauseParticlesWhenFrozen)
            {
                _particles = GetComponentsInChildren<ParticleSystem>(true);
                _particleWasPlaying = _particles != null && _particles.Length > 0 ? new bool[_particles.Length] : null;
            }
        }

        private void OnEnable()
        {
            SoulRealmManager.RegisterFreezeTarget(this);
        }

        private void OnDisable()
        {
            SoulRealmManager.UnregisterFreezeTarget(this);
            if (_frozen)
                ApplyFrozen(false);
        }

        public void ApplyFrozen(bool frozen)
        {
            if (_frozen == frozen)
                return;
            _frozen = frozen;

            if (animator != null)
            {
                if (frozen)
                {
                    _savedAnimatorSpeed = animator.speed;
                    animator.speed = 0f;
                }
                else
                {
                    animator.speed = _savedAnimatorSpeed > 0.001f ? _savedAnimatorSpeed : 1f;
                }
            }

            if (rigidBody != null)
            {
                if (frozen)
                {
                    rigidBody.linearVelocity = Vector3.zero;
                    rigidBody.angularVelocity = Vector3.zero;
                    if (kinematicWhenFrozen)
                        rigidBody.isKinematic = true;
                    else
                        rigidBody.Sleep();
                }
                else
                {
                    if (kinematicWhenFrozen)
                        rigidBody.isKinematic = false;
                    else
                        rigidBody.WakeUp();
                }
            }

            if (pauseParticlesWhenFrozen && _particles != null && _particleWasPlaying != null)
            {
                if (frozen)
                {
                    for (int i = 0; i < _particles.Length; i++)
                    {
                        var ps = _particles[i];
                        if (ps == null) continue;
                        bool playing = ps.isPlaying;
                        _particleWasPlaying[i] = playing;
                        if (playing)
                            ps.Pause(true);
                    }
                }
                else
                {
                    for (int i = 0; i < _particles.Length; i++)
                    {
                        var ps = _particles[i];
                        if (ps == null) continue;
                        if (_particleWasPlaying[i])
                            ps.Play(true);
                        _particleWasPlaying[i] = false;
                    }
                }
            }
        }
    }
}
