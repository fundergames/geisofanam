using UnityEngine;

namespace Geis.Puzzles
{
    /// <summary>
    /// Forwards trigger enter/exit to an owner (messages must live on the collider's GameObject).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SoulSwitchProximityRelay : MonoBehaviour
    {
        private IPuzzleProximityRelayOwner _owner;
        private bool _firesInteract;
        private bool _firesPrompt;

        public void Initialize(IPuzzleProximityRelayOwner owner, bool firesInteract, bool firesPrompt)
        {
            _owner = owner;
            _firesInteract = firesInteract;
            _firesPrompt = firesPrompt;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_owner == null || !SoulSwitchTrigger.IsPlayerProximityCollider(other))
                return;
            _owner.OnProximityRelayEnter(_firesInteract, _firesPrompt);
        }

        private void OnTriggerExit(Collider other)
        {
            if (_owner == null || !SoulSwitchTrigger.IsPlayerProximityCollider(other))
                return;
            _owner.OnProximityRelayExit(_firesInteract, _firesPrompt);
        }

        private void OnDestroy()
        {
            _owner = null;
        }
    }
}
