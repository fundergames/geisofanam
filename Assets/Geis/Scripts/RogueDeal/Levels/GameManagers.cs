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

namespace RogueDeal.Levels
{
    public class GameManagers : MonoBehaviour
    {
        private static GameManagers _instance;
        
        [Header("Manager References")]
        [SerializeField]
        private LevelManager levelManager;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeManagers();
        }

        private void InitializeManagers()
        {
            if (levelManager == null)
            {
                levelManager = GetComponentInChildren<LevelManager>();
                
                if (levelManager == null)
                {
                    GameObject levelManagerObj = new GameObject("LevelManager");
                    levelManagerObj.transform.SetParent(transform);
                    levelManager = levelManagerObj.AddComponent<LevelManager>();
                }
            }

            Debug.Log("[GameManagers] Initialized all game managers");
        }

        public static LevelManager GetLevelManager()
        {
            return _instance != null ? _instance.levelManager : null;
        }
    }
}
