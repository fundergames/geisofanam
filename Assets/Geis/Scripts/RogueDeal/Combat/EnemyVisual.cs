/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 *
 * This software and associated documentation files are proprietary and confidential.
 * Unauthorized copying, modification, distribution, or use of this software,
 * via any medium, is strictly prohibited without explicit written permission.
 *
 * This code is provided for personal use only by authorized recipients.
 * It may not be redistributed, sublicensed, or sold in any form.
 */

using Geis.Enemies;
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
            ResolveEnemyHealthBarReference();
        }

        private void Start()
        {
            WireEnemyHealthBar();

            // Support CombatEntity-only enemies (real-time combat without EnemyInstance)
            if (enemyInstance == null)
            {
                combatEntity = GetComponent<CombatEntity>() ?? GetComponentInParent<CombatEntity>() ?? GetComponentInChildren<CombatEntity>();
                if (combatEntity != null)
                    UpdateHealthBar(false);
            }
        }

        /// <summary>
        /// Prefer the world-space bar on a child object; a duplicate on the enemy root breaks follow/billboard.
        /// </summary>
        private void ResolveEnemyHealthBarReference()
        {
            if (enemyHealthBar != null)
                return;

            var bars = GetComponentsInChildren<EnemyHealthBar>(true);
            foreach (var bar in bars)
            {
                if (bar.transform != transform)
                {
                    enemyHealthBar = bar;
                    return;
                }
            }

            if (bars.Length > 0)
                enemyHealthBar = bars[0];
        }

        private void WireEnemyHealthBar()
        {
            ResolveEnemyHealthBarReference();
            if (enemyHealthBar == null)
                return;

            enemyHealthBar.SetFollowTarget(transform);
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
            
            ResolveEnemyHealthBarReference();

            CacheOriginalMaterials();

            enemyInstance = enemy;
            combatEntity = null;
            enemyInstance.visualInstance = gameObject;

            WireEnemyHealthBar();
            
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
                {
                    var combatant = combatEntity.GetComponent<EnemyCombatant>()
                        ?? combatEntity.GetComponentInParent<EnemyCombatant>()
                        ?? combatEntity.GetComponentInChildren<EnemyCombatant>();
                    enemyNameText.text = combatant != null && combatant.Definition != null
                        ? combatant.Definition.displayName
                        : combatEntity.gameObject.name;
                }
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
        
        /// <summary>Hit was blocked by immunity — show "Immune" instead of a damage number.</summary>
        public Sequence AnimateImmuneHit()
        {
            UpdateHealthBar();
            ShowImmunePopup();
            Transform targetTransform = modelRoot != null ? modelRoot : transform;
            Sequence sequence = DOTween.Sequence();
            sequence.Append(targetTransform.DOPunchScale(Vector3.one * 0.05f, 0.15f, 3, 0.5f));
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

        public void ResetPresentation()
        {
            Transform targetTransform = modelRoot != null ? modelRoot : transform;
            DOTween.Kill(targetTransform);
            targetTransform.localScale = Vector3.one;
            gameObject.SetActive(true);
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
        
        private void ShowImmunePopup()
        {
            Vector3 spawnPosition = damagePopupSpawnPoint != null
                ? damagePopupSpawnPoint.position
                : transform.position + damagePopupOffset;

            if (DamagePopupManager.Instance != null)
                DamagePopupManager.Instance.ShowImmunePopup(spawnPosition);
            else if (damagePopupPrefab != null)
            {
                GameObject popupObj = Instantiate(damagePopupPrefab, spawnPosition, Quaternion.identity);
                DamagePopup popup = popupObj.GetComponent<DamagePopup>();
                if (popup != null)
                    popup.InitializeImmune(spawnPosition, null);
            }
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
