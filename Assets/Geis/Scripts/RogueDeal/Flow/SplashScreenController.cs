using System.Threading.Tasks;
using UnityEngine;

namespace Funder.GameFlow
{
    public class SplashScreenController : MonoBehaviour
    {
        [SerializeField]
        private CanvasGroup canvasGroup;

        [SerializeField]
        private float fadeInDuration = 0.5f;

        [SerializeField]
        private float fadeOutDuration = 0.5f;

        private async void Start()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponentInChildren<CanvasGroup>();
            }

            if (canvasGroup != null)
            {
                await FadeIn();
            }
        }

        private async Task FadeIn()
        {
            float elapsed = 0f;
            canvasGroup.alpha = 0f;

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
                await Task.Yield();
            }

            canvasGroup.alpha = 1f;
        }

        public async Task FadeOut()
        {
            float elapsed = 0f;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeOutDuration);
                await Task.Yield();
            }

            canvasGroup.alpha = 0f;
        }
    }
}
