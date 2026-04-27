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
