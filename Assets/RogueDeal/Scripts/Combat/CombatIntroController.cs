using System.Collections;
using UnityEngine;
using DG.Tweening;
using RogueDeal.UI;

namespace RogueDeal.Combat
{
    public class CombatIntroController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerVisual playerVisual;
        [SerializeField] private Transform enemyContainer;
        [SerializeField] private CombatCameraController cameraController;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private LevelProgressIndicator progressIndicator;

        [Header("Intro Settings")]
        [SerializeField] private float playerRunSpeed = 3f;
        [SerializeField] private float fightDistance = 3f;
        [SerializeField] private Vector3 playerStartOffset = new Vector3(-10f, 0f, 0f);
        [SerializeField] private float enemyOffscreenOffset = 2f;

        private EnemyVisual currentEnemyVisual;
        private Vector3 fightPosition;
        private bool introComplete = false;
        private int currentEnemyIndex = 0;
        private CombatManager combatManager;

        public bool IntroComplete => introComplete;
        public Vector3 FightPosition => fightPosition;

        private void Awake()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (cameraController == null && mainCamera != null)
                cameraController = mainCamera.GetComponent<CombatCameraController>();

            if (progressIndicator == null)
                progressIndicator = FindFirstObjectByType<LevelProgressIndicator>();
        }

        public IEnumerator PlayIntro()
        {
            introComplete = false;
            currentEnemyIndex = 0;

            if (playerVisual == null)
            {
                Debug.LogWarning("[CombatIntroController] PlayerVisual not set!");
                introComplete = true;
                yield break;
            }

            currentEnemyVisual = GetCurrentEnemy();
            if (currentEnemyVisual == null)
            {
                Debug.LogWarning("[CombatIntroController] No enemy found!");
                introComplete = true;
                yield break;
            }

            if (mainCamera == null)
                mainCamera = Camera.main;

            Vector3 enemyPosition = currentEnemyVisual.transform.position;
            
            fightPosition = enemyPosition + Vector3.left * fightDistance;

            Vector3 playerStartPosition = fightPosition + playerStartOffset;
            playerVisual.transform.position = playerStartPosition;
            
            playerVisual.transform.rotation = Quaternion.Euler(0, 90, 0);
            
            if (currentEnemyVisual != null)
            {
                currentEnemyVisual.transform.rotation = Quaternion.Euler(0, -90, 0);
            }

            if (cameraController != null)
            {
                cameraController.SetTarget(playerVisual.transform);
                cameraController.SnapToTarget();
                cameraController.SetFollowEnabled(true);
            }

            if (playerVisual.Animator != null && HasAnimatorParameter(playerVisual.Animator, "Run"))
            {
                playerVisual.Animator.SetBool("Run", true);
            }

            float distance = Vector3.Distance(playerStartPosition, fightPosition);
            float duration = distance / playerRunSpeed;

            yield return MovePlayerWithProgress(playerStartPosition, fightPosition, duration, 0);

            if (playerVisual.Animator != null && HasAnimatorParameter(playerVisual.Animator, "Run"))
            {
                playerVisual.Animator.SetBool("Run", false);
            }

            introComplete = true;
        }
        
        private bool HasAnimatorParameter(Animator animator, string paramName)
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

        private float GetCameraHalfWidth()
        {
            if (mainCamera == null)
                return 5f;

            float distance = Mathf.Abs(mainCamera.transform.position.z);
            float halfHeight = mainCamera.orthographicSize > 0 
                ? mainCamera.orthographicSize 
                : distance * Mathf.Tan(mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            
            return halfHeight * mainCamera.aspect;
        }

        public void SetPlayerVisual(PlayerVisual visual)
        {
            playerVisual = visual;
        }

        public void SetEnemyContainer(Transform container)
        {
            enemyContainer = container;
        }

        public void SetCameraController(CombatCameraController controller)
        {
            cameraController = controller;
        }

        public void SetProgressIndicator(LevelProgressIndicator indicator)
        {
            progressIndicator = indicator;
        }

        public void SetCombatManager(CombatManager manager)
        {
            combatManager = manager;
        }

        private EnemyVisual GetCurrentEnemy()
        {
            if (enemyContainer != null)
            {
                return enemyContainer.GetComponentInChildren<EnemyVisual>();
            }
            return null;
        }

        public IEnumerator RunToNextEnemy()
        {
            currentEnemyIndex++;
            
            currentEnemyVisual = GetCurrentEnemy();
            if (currentEnemyVisual == null || playerVisual == null)
            {
                yield break;
            }

            Vector3 enemyPosition = currentEnemyVisual.transform.position;
            Vector3 newFightPosition = enemyPosition + Vector3.left * fightDistance;
            Vector3 startPosition = playerVisual.transform.position;

            if (playerVisual.Animator != null)
            {
                playerVisual.Animator.SetBool("Running", true);
            }

            float distance = Vector3.Distance(startPosition, newFightPosition);
            float duration = distance / playerRunSpeed;

            if (duration > 0.1f)
            {
                yield return MovePlayerWithProgress(startPosition, newFightPosition, duration, currentEnemyIndex);
            }

            if (playerVisual.Animator != null)
            {
                playerVisual.Animator.SetBool("Running", false);
            }

            fightPosition = newFightPosition;
        }

        private IEnumerator MovePlayerWithProgress(Vector3 startPos, Vector3 endPos, float duration, int toEnemyIndex)
        {
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                playerVisual.transform.position = Vector3.Lerp(startPos, endPos, t);
                
                if (progressIndicator != null)
                {
                    progressIndicator.SetProgressFromEnemyIndex(toEnemyIndex, t);
                }
                
                yield return null;
            }
            
            playerVisual.transform.position = endPos;
            
            if (progressIndicator != null)
            {
                progressIndicator.SetProgressFromEnemyIndex(toEnemyIndex, 1f);
            }
        }
    }
}
