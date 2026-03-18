using UnityEngine;

namespace FunderGames.UI
{
    public class UIWindowComponents : MonoBehaviour
    {
        public RectTransform RectTransform { get; private set; }
        public CanvasGroup CanvasGroup { get; private set; }

        private void Awake()
        {
            RectTransform = GetComponent<RectTransform>();
            CanvasGroup = GetComponent<CanvasGroup>();
        }
    }
}