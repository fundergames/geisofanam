using UnityEngine;
using UnityEngine.InputSystem;
using RogueDeal.Events;

namespace RogueDeal.Player
{
    public class PlayerInteractionController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool pausePlayerMovementDuringDialog = true;
        
        private PlayerInput _playerInput;
        private bool _isDialogActive = false;
        
        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
        }

        private void Start()
        {
            EventBus<DialogStartedEvent>.Subscribe(OnDialogStarted);
            EventBus<DialogEndedEvent>.Subscribe(OnDialogEnded);
        }

        private void OnDestroy()
        {
            EventBus<DialogStartedEvent>.Unsubscribe(OnDialogStarted);
            EventBus<DialogEndedEvent>.Unsubscribe(OnDialogEnded);
        }

        private void OnDialogStarted(DialogStartedEvent evt)
        {
            _isDialogActive = true;
            
            if (pausePlayerMovementDuringDialog && _playerInput != null)
            {
                _playerInput.DeactivateInput();
            }
        }

        private void OnDialogEnded(DialogEndedEvent evt)
        {
            _isDialogActive = false;
            
            if (pausePlayerMovementDuringDialog && _playerInput != null)
            {
                _playerInput.ActivateInput();
            }
        }

        public bool IsDialogActive()
        {
            return _isDialogActive;
        }
    }
}
