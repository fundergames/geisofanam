using UnityEngine;

namespace RogueDeal.UI
{
    public class PanelComponents : MonoBehaviour
    {
        public RectTransform RectTransform { get; private set; }
        public CanvasGroup CanvasGroup { get; private set; }

        private void Awake()
        {
            RectTransform = GetComponent<RectTransform>();
            CanvasGroup = GetComponent<CanvasGroup>();
            
            if (CanvasGroup == null)
            {
                CanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
    }
}
