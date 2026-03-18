using UnityEngine;
using UnityEngine.InputSystem;

namespace RogueDeal.UI
{
    public class DebugUIToggle : MonoBehaviour
    {
        [Header("Targets to Toggle")]
        [SerializeField] private GameObject combatFlowDebugger;
        [SerializeField] private GameObject analyticsDemo;

        [Header("Settings")]
        [SerializeField] private Key toggleKey = Key.Backquote;
        [SerializeField] private bool startHidden = false;

        private Keyboard keyboard;

        private void Start()
        {
            keyboard = Keyboard.current;

            if (combatFlowDebugger == null)
            {
                GameObject debuggerParent = GameObject.Find("Debugger");
                if (debuggerParent != null)
                {
                    Transform child = debuggerParent.transform.Find("CombatFlowDebugger");
                    if (child != null)
                        combatFlowDebugger = child.gameObject;
                }
            }

            if (analyticsDemo == null)
            {
                GameObject exampleServices = GameObject.Find("ExampleServices");
                if (exampleServices != null)
                {
                    analyticsDemo = exampleServices;
                }
            }

            if (startHidden)
            {
                SetVisibility(false);
            }
        }

        private void Update()
        {
            if (keyboard != null && keyboard[toggleKey].wasPressedThisFrame)
            {
                ToggleVisibility();
            }
        }

        private void ToggleVisibility()
        {
            bool newState = combatFlowDebugger != null ? !combatFlowDebugger.activeSelf : true;
            SetVisibility(newState);
        }

        private void SetVisibility(bool visible)
        {
            if (combatFlowDebugger != null)
            {
                combatFlowDebugger.SetActive(visible);
            }

            if (analyticsDemo != null)
            {
                analyticsDemo.SetActive(visible);
            }
        }
    }
}
