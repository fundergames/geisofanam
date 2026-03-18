using UnityEngine;
using UnityEngine.InputSystem;

namespace RogueDeal.Combat.Training
{
    public class TrainingInputRecorder : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ComboRecorder comboRecorder;
        [SerializeField] private TrainingModeManager trainingManager;
        
        [Header("Input Actions to Record")]
        [SerializeField] private InputActionReference attackAction;
        [SerializeField] private InputActionReference specialAction;
        [SerializeField] private InputActionReference blockAction;
        [SerializeField] private InputActionReference dodgeAction;
        
        private void Awake()
        {
            if (comboRecorder == null)
            {
                comboRecorder = GetComponent<ComboRecorder>();
            }
            
            if (trainingManager == null)
            {
                trainingManager = GetComponent<TrainingModeManager>();
            }
        }
        
        private void OnEnable()
        {
            SubscribeToActions();
        }
        
        private void OnDisable()
        {
            UnsubscribeFromActions();
        }
        
        private void SubscribeToActions()
        {
            if (attackAction != null && attackAction.action != null)
            {
                attackAction.action.started += OnAttackInput;
                attackAction.action.performed += OnAttackInput;
                attackAction.action.canceled += OnAttackInput;
            }
            
            if (specialAction != null && specialAction.action != null)
            {
                specialAction.action.started += OnSpecialInput;
                specialAction.action.performed += OnSpecialInput;
                specialAction.action.canceled += OnSpecialInput;
            }
            
            if (blockAction != null && blockAction.action != null)
            {
                blockAction.action.started += OnBlockInput;
                blockAction.action.performed += OnBlockInput;
                blockAction.action.canceled += OnBlockInput;
            }
            
            if (dodgeAction != null && dodgeAction.action != null)
            {
                dodgeAction.action.started += OnDodgeInput;
                dodgeAction.action.performed += OnDodgeInput;
                dodgeAction.action.canceled += OnDodgeInput;
            }
        }
        
        private void UnsubscribeFromActions()
        {
            if (attackAction != null && attackAction.action != null)
            {
                attackAction.action.started -= OnAttackInput;
                attackAction.action.performed -= OnAttackInput;
                attackAction.action.canceled -= OnAttackInput;
            }
            
            if (specialAction != null && specialAction.action != null)
            {
                specialAction.action.started -= OnSpecialInput;
                specialAction.action.performed -= OnSpecialInput;
                specialAction.action.canceled -= OnSpecialInput;
            }
            
            if (blockAction != null && blockAction.action != null)
            {
                blockAction.action.started -= OnBlockInput;
                blockAction.action.performed -= OnBlockInput;
                blockAction.action.canceled -= OnBlockInput;
            }
            
            if (dodgeAction != null && dodgeAction.action != null)
            {
                dodgeAction.action.started -= OnDodgeInput;
                dodgeAction.action.performed -= OnDodgeInput;
                dodgeAction.action.canceled -= OnDodgeInput;
            }
        }
        
        private void OnAttackInput(InputAction.CallbackContext context)
        {
            RecordInput("Attack", context);
        }
        
        private void OnSpecialInput(InputAction.CallbackContext context)
        {
            RecordInput("Special", context);
        }
        
        private void OnBlockInput(InputAction.CallbackContext context)
        {
            RecordInput("Block", context);
        }
        
        private void OnDodgeInput(InputAction.CallbackContext context)
        {
            RecordInput("Dodge", context);
        }
        
        private void RecordInput(string actionName, InputAction.CallbackContext context)
        {
            if (trainingManager != null && trainingManager.IsTrainingMode && comboRecorder != null)
            {
                comboRecorder.RecordInput(actionName, context);
            }
        }
    }
}
