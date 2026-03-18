using UnityEngine;
using TMPro;

namespace RogueDeal.UI
{
    public class InteractionPromptUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI promptText;
        [SerializeField] private string defaultText = "Press [E] to interact";
        
        [Header("Animation")]
        [SerializeField] private bool bobAnimation = true;
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float bobHeight = 0.2f;
        
        private Vector3 _initialPosition;
        private Camera _mainCamera;
        
        private void Start()
        {
            _initialPosition = transform.localPosition;
            _mainCamera = Camera.main;
            
            if (promptText != null)
            {
                promptText.text = defaultText;
            }
        }
        
        private void Update()
        {
            if (_mainCamera != null)
            {
                transform.LookAt(transform.position + _mainCamera.transform.rotation * Vector3.forward,
                    _mainCamera.transform.rotation * Vector3.up);
            }
            
            if (bobAnimation)
            {
                float newY = _initialPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
                transform.localPosition = new Vector3(_initialPosition.x, newY, _initialPosition.z);
            }
        }
        
        public void SetText(string text)
        {
            if (promptText != null)
            {
                promptText.text = text;
            }
        }
    }
}
