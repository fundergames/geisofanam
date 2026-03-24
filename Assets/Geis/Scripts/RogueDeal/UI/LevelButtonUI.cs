using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RogueDeal.Levels;

namespace RogueDeal.UI
{
    public class LevelButtonUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField]
        private Button button;

        [SerializeField]
        private TextMeshProUGUI levelNumberText;

        [SerializeField]
        private Image backgroundImage;

        [SerializeField]
        private GameObject[] starIcons;

        [SerializeField]
        private GameObject lockedIcon;

        [SerializeField]
        private GameObject completedIcon;

        [Header("Visual States")]
        [SerializeField]
        private Color unlockedColor = Color.white;

        [SerializeField]
        private Color lockedColor = Color.gray;

        [SerializeField]
        private Color selectedColor = Color.yellow;

        [SerializeField]
        private float selectedScale = 1.1f;

        private LevelDefinition _level;
        private bool _isUnlocked;
        private int _stars;
        private Action _onClickCallback;
        private bool _isSelected;
        private Vector3 _originalScale;

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            _originalScale = transform.localScale;

            if (button != null)
            {
                button.onClick.AddListener(OnButtonClicked);
            }
        }

        public void Initialize(LevelDefinition level, bool isUnlocked, int stars, Action onClickCallback)
        {
            _level = level;
            _isUnlocked = isUnlocked;
            _stars = stars;
            _onClickCallback = onClickCallback;

            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (levelNumberText != null)
            {
                levelNumberText.text = _level.levelNumber.ToString();
            }

            if (button != null)
            {
                button.interactable = _isUnlocked;
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = _isUnlocked ? unlockedColor : lockedColor;
            }

            if (lockedIcon != null)
            {
                lockedIcon.SetActive(!_isUnlocked);
            }

            if (completedIcon != null)
            {
                completedIcon.SetActive(_stars > 0);
            }

            UpdateStars();
        }

        private void UpdateStars()
        {
            if (starIcons == null || starIcons.Length == 0)
                return;

            for (int i = 0; i < starIcons.Length; i++)
            {
                if (starIcons[i] != null)
                {
                    starIcons[i].SetActive(i < _stars);
                }
            }
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;

            if (_isSelected)
            {
                transform.localScale = _originalScale * selectedScale;
                
                if (backgroundImage != null)
                {
                    backgroundImage.color = _isUnlocked ? selectedColor : lockedColor;
                }
            }
            else
            {
                transform.localScale = _originalScale;
                
                if (backgroundImage != null)
                {
                    backgroundImage.color = _isUnlocked ? unlockedColor : lockedColor;
                }
            }
        }

        private void OnButtonClicked()
        {
            if (_isUnlocked && _onClickCallback != null)
            {
                _onClickCallback.Invoke();
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnButtonClicked);
            }
        }
    }
}
