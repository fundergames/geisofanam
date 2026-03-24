using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Funder.GameFlow
{
    public class EventSystemManager : MonoBehaviour
    {
        private static EventSystemManager _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Additive)
            {
                EnsureSingleEventSystem();
            }
        }

        private void EnsureSingleEventSystem()
        {
            var allEventSystems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);

            if (allEventSystems.Length <= 1)
                return;

            EventSystem activeEventSystem = EventSystem.current;
            
            foreach (var eventSystem in allEventSystems)
            {
                if (eventSystem != activeEventSystem)
                {
                    Debug.Log($"[EventSystemManager] Disabling duplicate EventSystem in scene: {eventSystem.gameObject.scene.name}");
                    eventSystem.enabled = false;
                }
            }
        }
    }
}
