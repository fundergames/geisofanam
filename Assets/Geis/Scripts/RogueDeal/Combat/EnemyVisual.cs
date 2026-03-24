using RogueDeal.Enemies;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace RogueDeal.Combat
{
    public class EnemyVisual : MonoBehaviour
    {
        [Header("3D Model")]
        [SerializeField] private Transform modelRoot;
        [SerializeField] private Animator animator;
        [SerializeField] private Renderer[] renderers;
        
        [Header("UI Components (World Space or Screen Space)")]
        [SerializeField] private Canvas uiCanvas;
        [SerializeField] private TextMeshProUGUI enemyNameText;
        [SerializeField] private Slider healthBar;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private EnemyHealthBar enemyHealthBar;
        
        [Header("Damage Popup")]
        [SerializeField] private GameObject damagePopupPrefab;
        [SerializeField] private Transform damagePopupSpawnPoint;
        [SerializeField] private Vector3 damagePopupOffset = new Vector3(0f, 2f, 0f);
        
        [Header("Animation")]
        [SerializeField] private float spawnDuration = 0.5f;
        [SerializeField] private Vector3 spawnScale = new Vector3(0.5f, 0.5f, 0.5f);
        
        private EnemyInstance enemyInstance;
        private CombatEntity combatEntity;
        private Material[] originalMaterials;
        private Vector3 enemyStartPosition;
        
        public EnemyInstance EnemyInstance => enemyInstance;
        public Animator Animator => animator;
        public Vector3 EnemyStartPosition => enemyStartPosition;
        
        private void Awake()
        {
            enemyStartPosition = transform.position;
            
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
        }

        private void Start()
        {
            if (enemyHealthBar != null && enemyHealthBar.transform.parent == transform)
            {
                enemyHealthBar.SetFollowTarget(transform);
            }
            
            // Support CombatEntity-only enemies (real-time combat without EnemyInstance)
            if (enemyInstance == null)
            {
                combatEntity = GetComponent<CombatEntity>() ?? GetComponentInParent<CombatEntity>() ?? GetComponentInChildren<CombatEntity>();
                if (combatEntity != null)
                {
                    enemyHealthBar?.SetCombatEntity(combatEntity);
                    UpdateHealthBar(false);
                }
            }
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
        
        public void Initialize(EnemyInstance enemy)
        {
            enemyStartPosition = transform.position;
            
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
            
            if (enemyHealthBar == null)
                enemyHealthBar = GetComponentInChildren<EnemyHealthBar>();
            
            CacheOriginalMaterials();
            
            enemyInstance = enemy;
            combatEntity = null;
            enemyInstance.visualInstance = gameObject;
            
            if (enemyHealthBar != null)
            {
                enemyHealthBar.SetFollowTarget(transform);
            }
            
            UpdateVisuals();
        }
        
        public void SetStartPosition(Vector3 position)
        {
            enemyStartPosition = position;
        }
        
        public void UpdateVisuals()
        {
            if (enemyInstance != null)
            {
                if (enemyNameText != null)
                    enemyNameText.text = enemyInstance.definition.displayName;
            }
            else if (combatEntity != null)
            {
                if (enemyNameText != null)
                    enemyNameText.text = combatEntity.gameObject.name;
            }
            else
            {
                return;
            }
            
            UpdateHealthBar(false);
        }
        
        public void UpdateHealthBar(bool animate = true)
        {
            float currentHealth, maxHealth;
            
            if (enemyInstance != null)
            {
                currentHealth = enemyInstance.stats.currentHealth;
                maxHealth = enemyInstance.stats.maxHealth;
            }
            else if (combatEntity != null)
            {
                var data = combatEntity.GetEntityData();
                if (data == null) return;
                currentHealth = data.currentHealth;
                maxHealth = data.maxHealth;
            }
            else
            {
                return;
            }
            
            if (maxHealth <= 0) return;
            float healthPercent = currentHealth / maxHealth;
            
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
                healthText.text = $"{Mathf.RoundToInt(currentHealth)} / {Mathf.RoundToInt(maxHealth)}";
            }
            
            if (enemyHealthBar != null)
            {
                enemyHealthBar.UpdateHealthBar(currentHealth, maxHealth, animate);
            }
        }
        
        public Sequence AnimateSpawn()
        {
            Transform targetTransform = modelRoot != null ? modelRoot : transform;
            targetTransform.localScale = spawnScale;
            
            Sequence sequence = DOTween.Sequence();
            sequence.Append(targetTransform.DOScale(Vector3.one, spawnDuration).SetEase(Ease.OutBack));
            
            if (animator != null)
            {
                animator.SetTrigger("Spawn");
            }
            
            return sequence;
        }
        
        public Sequence AnimateDamage(int damageAmount, bool isCritical = false)
        {
            UpdateHealthBar();
            
            ShowDamagePopup(damageAmount, isCritical);
            
            Transform targetTransform = modelRoot != null ? modelRoot : transform;
            Sequence sequence = DOTween.Sequence();
            
            sequence.Append(targetTransform.DOPunchScale(Vector3.one * 0.1f, 0.3f, 5, 0.5f));
            
            FlashRed();
            
            return sequence;
        }
        
        public Sequence AnimateDefeat()
        {
            Transform targetTransform = modelRoot != null ? modelRoot : transform;
            Sequence sequence = DOTween.Sequence();
            
            sequence.Append(targetTransform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack));
            
            sequence.AppendCallback(() => {
                gameObject.SetActive(false);
            });
            
            return sequence;
        }
        
        public void TriggerAnimation(string triggerName)
        {
            if (animator != null)
            {
                animator.SetTrigger(triggerName);
            }
        }

        [ContextMenu("Test Normal Damage")]
        private void TestNormalDamage()
        {
            ShowDamagePopup(Random.Range(10, 50), false);
        }

        [ContextMenu("Test Critical Damage")]
        private void TestCriticalDamage()
        {
            ShowDamagePopup(Random.Range(50, 100), true);
        }
        
        private void ShowDamagePopup(int damageAmount, bool isCritical)
        {
            Vector3 spawnPosition = damagePopupSpawnPoint != null 
                ? damagePopupSpawnPoint.position 
                : transform.position + damagePopupOffset;

            if (DamagePopupManager.Instance != null)
            {
                DamagePopupManager.Instance.ShowDamagePopup(damageAmount, isCritical, spawnPosition);
            }
            else if (damagePopupPrefab != null)
            {
                GameObject popupObj = Instantiate(damagePopupPrefab, spawnPosition, Quaternion.identity);
                DamagePopup popup = popupObj.GetComponent<DamagePopup>();
                
                if (popup != null)
                {
                    popup.Initialize(damageAmount, isCritical, spawnPosition);
                }
            }
        }
        
        private static readonly string[] ColorPropertyNames = { "_Color", "_BaseColor", "_MainColor" };

        private void FlashRed()
        {
            if (renderers == null || renderers.Length == 0)
                return;

            foreach (var renderer in renderers)
            {
                if (renderer == null)
                    continue;

                Material mat = renderer.material;
                string colorProp = GetColorPropertyName(mat);
                if (string.IsNullOrEmpty(colorProp))
                    continue;

                Color originalColor = mat.GetColor(colorProp);
                Sequence flashSequence = DOTween.Sequence();
                flashSequence.Append(mat.DOColor(Color.red, colorProp, 0.1f).SetTarget(mat));
                flashSequence.Append(mat.DOColor(originalColor, colorProp, 0.1f).SetTarget(mat));
            }
        }

        private static string GetColorPropertyName(Material mat)
        {
            if (mat == null)
                return null;
            foreach (string name in ColorPropertyNames)
            {
                if (mat.HasProperty(name))
                    return name;
            }
            return null;
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
