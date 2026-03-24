using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace Geis.InputSystem
{
    /// <summary>
    /// Separates "gameplay" from "menu" use of the gamepad B / east face button:
    /// the default <see cref="InputSystemUIInputModule"/> Cancel action binds to the same control as UI Cancel
    /// (<c>*/{Cancel}</c>), which blocks Player/Dodge on <c>&lt;Gamepad&gt;/buttonEast</c>.
    /// <list type="bullet">
    /// <item><description><b>Gameplay</b> — clear the UI module's Cancel binding so B is only used by GeisControls (dodge).</description></item>
    /// <item><description><b>Menus / pause</b> — restore Cancel so B closes menus and navigates UI as usual.</description></item>
    /// </list>
    /// Call <see cref="SetMenuNavigationActive"/> from pause panels, inventory, etc., or rely on
    /// <see cref="applyGameplayOnStart"/> for combat scenes with no menu open at start.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GeisUIInputRouting : MonoBehaviour
    {
        [SerializeField] private InputSystemUIInputModule uiModule;
        [Tooltip("Drag UI/Cancel from Assets/InputSystem_Actions if auto-capture from the module fails.")]
        [SerializeField] private InputActionReference menuCancelActionOverride;

        [Tooltip("On Start, call SetMenuNavigationActive(false) so B is free for dodge.")]
        [SerializeField] private bool applyGameplayOnStart = true;

        private InputActionReference _storedCancelReference;
        private bool _menuNavigationActive;
        private static GeisUIInputRouting _instance;

        public static GeisUIInputRouting Instance => _instance;

        public bool MenuNavigationActive => _menuNavigationActive;

        private void OnEnable()
        {
            _instance = this;
        }

        private void OnDisable()
        {
            if (_instance == this)
                _instance = null;
        }

        private void Awake()
        {
            if (uiModule == null)
                uiModule = FindInputModule();
        }

        private void Start()
        {
            if (uiModule == null)
                uiModule = FindInputModule();

            if (uiModule == null)
            {
                Debug.LogWarning(
                    "[GeisUIInputRouting] No InputSystemUIInputModule found. Assign uiModule or add this to the EventSystem object.",
                    this);
                return;
            }

            if (menuCancelActionOverride != null)
                _storedCancelReference = menuCancelActionOverride;
            else if (uiModule.cancel != null)
                _storedCancelReference = uiModule.cancel;

            if (applyGameplayOnStart)
                SetMenuNavigationActive(false);
        }

        /// <param name="menuOpen">
        /// True while a menu, pause, or other UI should receive gamepad Cancel (B). False during gameplay so dodge can use B.
        /// </param>
        public void SetMenuNavigationActive(bool menuOpen)
        {
            _menuNavigationActive = menuOpen;

            if (uiModule == null)
                uiModule = FindInputModule();
            if (uiModule == null)
                return;

            if (menuOpen)
            {
                if (_storedCancelReference != null)
                    uiModule.cancel = _storedCancelReference;
            }
            else
                uiModule.cancel = null;
        }

        private static InputSystemUIInputModule FindInputModule()
        {
#if UNITY_2022_2_OR_NEWER
            return Object.FindFirstObjectByType<InputSystemUIInputModule>();
#else
            return Object.FindObjectOfType<InputSystemUIInputModule>();
#endif
        }
    }
}
