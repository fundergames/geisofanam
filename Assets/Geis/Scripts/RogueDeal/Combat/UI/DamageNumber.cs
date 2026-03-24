using UnityEngine;
using TMPro;
using System.Collections;

namespace RogueDeal.Combat.UI
{
    public class DamageNumber : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI damageText;
        [SerializeField] private float lifetime = 1f;
        [SerializeField] private float floatSpeed = 50f;
        [SerializeField] private AnimationCurve fadeOverTime = AnimationCurve.EaseInOut(0, 1, 1, 0);

        private Vector3 worldPosition;
        private Camera cam;
        private CanvasGroup canvasGroup;
        private float timer;

        public void Initialize(float damage, bool isCritical, Vector3 worldPos, Camera camera)
        {
            worldPosition = worldPos;
            cam = camera;
            
            if (damageText != null)
            {
                damageText.text = Mathf.RoundToInt(damage).ToString();
                damageText.fontSize = isCritical ? 48 : 36;
                damageText.color = isCritical ? Color.yellow : Color.white;
            }
            
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            StartCoroutine(AnimateNumber());
        }

        private IEnumerator AnimateNumber()
        {
            while (timer < lifetime)
            {
                timer += Time.deltaTime;
                float progress = timer / lifetime;

                worldPosition += Vector3.up * floatSpeed * Time.deltaTime;
                
                if (cam != null)
                {
                    Vector3 screenPos = cam.WorldToScreenPoint(worldPosition);
                    transform.position = screenPos;
                }

                if (canvasGroup != null)
                    canvasGroup.alpha = fadeOverTime.Evaluate(progress);

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
