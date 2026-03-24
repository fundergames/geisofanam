using RogueDeal.Player;
using RogueDeal.Combat.Visual;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace RogueDeal.Combat
{
    public class PlayerVisual : MonoBehaviour
    {
        [Header("3D Model")]
        [SerializeField] private Transform modelRoot;
        [SerializeField] private Animator animator;
        [SerializeField] private Renderer[] renderers;
        
        [Header("Modular Character System")]
        [Tooltip("Character visual data for modular character system (optional)")]
        [SerializeField] private CharacterVisualData characterVisualData;
        
        [Tooltip("Character visual manager component (auto-found if not assigned)")]
        [SerializeField] private CharacterVisualManager characterVisualManager;
        
        [Header("UI Components (World Space or Screen Space)")]
        [SerializeField] private Canvas uiCanvas;
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private Slider healthBar;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private Slider xpBar;
        [SerializeField] private TextMeshProUGUI levelText;
        
        [Header("Animation")]
        [SerializeField] private float spawnDuration = 0.5f;
        [SerializeField] private Vector3 spawnScale = new Vector3(0.5f, 0.5f, 0.5f);
        
        private PlayerCharacter playerCharacter;
        private Material[] originalMaterials;
        private Vector3 originalModelLocalPosition;
        
        public PlayerCharacter PlayerCharacter => playerCharacter;
        public Animator Animator => animator;
        public Transform ModelRoot => modelRoot;
        
        private void Awake()
        {
            Debug.Log($"[PlayerVisual] Awake on {gameObject.name}");
            
            if (modelRoot == null)
            {
                Transform modelChild = transform.Find("Model");
                if (modelChild != null)
                {
                    modelRoot = modelChild;
                }
                else
                {
                    modelRoot = transform;
                }
            }
            
            if (renderers == null || renderers.Length == 0)
                renderers = GetComponentsInChildren<Renderer>();
            
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
            
            CacheOriginalMaterials();
            
            Debug.Log($"[PlayerVisual] Awake complete - renderers: {renderers?.Length ?? 0}");
        }
        
        private void CacheOriginalMaterials()
        {
            if (renderers == null || renderers.Length == 0)
                return;
            
            originalMaterials = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    originalMaterials[i] = renderers[i].material;
            }
        }
        
        public void Initialize(PlayerCharacter player)
        {
            if (modelRoot == null)
            {
                Transform modelChild = transform.Find("Model");
                if (modelChild != null)
                {
                    modelRoot = modelChild;
                }
                else
                {
                    modelRoot = transform;
                }
            }
            
            if (renderers == null || renderers.Length == 0)
                renderers = GetComponentsInChildren<Renderer>();
            
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
            
            CacheOriginalMaterials();
            
            playerCharacter = player;
            
            if (modelRoot != null)
            {
                originalModelLocalPosition = modelRoot.localPosition;
                Debug.Log($"[PlayerVisual] Cached model local position: {originalModelLocalPosition}");
            }
            
            SetupAnimator();
            
            // Initialize modular character system if available
            InitializeModularCharacterSystem();
            
            UpdateVisuals();
        }
        
        private void InitializeModularCharacterSystem()
        {
            // Find CharacterVisualManager if not assigned
            if (characterVisualManager == null)
            {
                characterVisualManager = GetComponent<CharacterVisualManager>();
                if (characterVisualManager == null)
                {
                    characterVisualManager = GetComponentInChildren<CharacterVisualManager>();
                }
            }
            
            // Initialize with visual data if available
            if (characterVisualManager != null)
            {
                CharacterVisualData visualData = characterVisualData;
                
                // Try to get from player character if not set
                if (visualData == null && playerCharacter != null)
                {
                    // Could load from player character data if that system exists
                    // For now, use the serialized field
                }
                
                if (visualData != null)
                {
                    characterVisualManager.Initialize(visualData, playerCharacter);
                    Debug.Log($"[PlayerVisual] Initialized modular character system with: {visualData.name}");
                }
            }
        }
        
        private void SetupAnimator()
        {
            if (animator == null)
            {
                Debug.LogWarning("[PlayerVisual] No Animator found on player model!");
                return;
            }
            
            if (playerCharacter?.classDefinition?.animatorData == null)
            {
                Debug.LogWarning("[PlayerVisual] No animator data found in class definition!");
                return;
            }
            
            var animData = playerCharacter.classDefinition.animatorData;
            
            if (animData.battleAnimator != null)
            {
                animator.runtimeAnimatorController = animData.battleAnimator;
                Debug.Log($"[PlayerVisual] Applied battle animator controller: {animData.battleAnimator.name}");
            }
            else
            {
                Debug.LogWarning("[PlayerVisual] Battle animator controller is null in animator data!");
            }
        }
        
        public void UpdateVisuals()
        {
            if (playerCharacter == null)
                return;
            
            if (playerNameText != null)
                playerNameText.text = playerCharacter.characterName;
            
            if (levelText != null)
                levelText.text = $"Lv. {playerCharacter.level}";
            
            UpdateHealthBar(false);
            UpdateXPBar();
        }
        
        public void UpdateHealthBar(bool animate = true)
        {
            if (playerCharacter == null)
                return;
            
            float healthPercent = (float)playerCharacter.effectiveStats.currentHealth / playerCharacter.effectiveStats.maxHealth;
            
            if (healthBar != null)
            {
                if (animate && Application.isPlaying)
                {
                    DOTween.To(() => healthBar.value, x => healthBar.value = x, healthPercent, 0.3f).SetEase(Ease.OutQuad);
                }
                else
                {
                    healthBar.value = healthPercent;
                }
            }
            
            if (healthText != null)
            {
                healthText.text = $"{playerCharacter.effectiveStats.currentHealth} / {playerCharacter.effectiveStats.maxHealth}";
            }
        }
        
        public void UpdateXPBar()
        {
            if (playerCharacter == null || xpBar == null)
                return;
            
            int xpToNextLevel = playerCharacter.classDefinition.GetXPForLevel(playerCharacter.level + 1);
            float xpPercent = (float)playerCharacter.currentXP / xpToNextLevel;
            xpBar.value = xpPercent;
        }
        
        public Sequence AnimateSpawn()
        {
            Transform targetTransform = modelRoot != null ? modelRoot : transform;
            targetTransform.localScale = spawnScale;
            
            Sequence sequence = DOTween.Sequence();
            sequence.Append(targetTransform.DOScale(Vector3.one, spawnDuration).SetEase(Ease.OutBack));
            
            if (animator != null)
            {
                if (HasAnimatorParameter("Spawn"))
                {
                    animator.SetTrigger("Spawn");
                }
            }
            
            return sequence;
        }
        
        private bool HasAnimatorParameter(string paramName)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
                return false;
                
            foreach (var param in animator.parameters)
            {
                if (param.name == paramName)
                    return true;
            }
            return false;
        }
        
        public Sequence AnimateDamage(int damageAmount)
        {
            UpdateHealthBar();
            
            Transform targetTransform = modelRoot != null ? modelRoot : transform;
            Sequence sequence = DOTween.Sequence();
            
            sequence.Append(targetTransform.DOPunchScale(Vector3.one * 0.1f, 0.3f, 5, 0.5f));
            
            FlashRed();
            
            return sequence;
        }
        
        public void TriggerAnimation(string triggerName)
        {
            if (animator != null)
            {
                animator.SetTrigger(triggerName);
            }
        }
        
        public void ResetModelLocalPosition()
        {
            if (modelRoot != null)
            {
                Debug.Log($"[PlayerVisual] Resetting model local position from {modelRoot.localPosition} to {originalModelLocalPosition}");
                modelRoot.localPosition = originalModelLocalPosition;
            }
        }
        
        private void FlashRed()
        {
            if (renderers == null || renderers.Length == 0)
                return;
            
            foreach (var renderer in renderers)
            {
                if (renderer == null)
                    continue;
                
                Material mat = renderer.material;
                Color originalColor = mat.color;
                
                Sequence flashSequence = DOTween.Sequence();
                flashSequence.Append(mat.DOColor(Color.red, 0.1f));
                flashSequence.Append(mat.DOColor(originalColor, 0.1f));
            }
        }
        
        private void OnDestroy()
        {
            Transform targetTransform = modelRoot != null ? modelRoot : transform;
            DOTween.Kill(targetTransform);
            
            if (renderers != null)
            {
                foreach (var renderer in renderers)
                {
                    if (renderer != null && renderer.material != null)
                        DOTween.Kill(renderer.material);
                }
            }
        }
    }
}
