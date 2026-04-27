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

namespace RogueDeal.Combat
{
    public class CombatCameraSetup : MonoBehaviour
    {
        [Header("Camera Position")]
        [SerializeField] private Vector3 cameraPosition = new Vector3(0f, 5f, -8f);
        [SerializeField] private Vector3 lookAtPosition = new Vector3(0f, 1f, 0f);
        
        private void Start()
        {
            SetupCamera();
        }
        
        private void SetupCamera()
        {
            Camera mainCamera = Camera.main;
            
            if (mainCamera == null)
            {
                Debug.LogError("[CombatCameraSetup] Main Camera not found!");
                return;
            }
            
            mainCamera.transform.position = cameraPosition;
            mainCamera.transform.LookAt(lookAtPosition);
            
            Debug.Log($"[CombatCameraSetup] Camera positioned at {cameraPosition}, looking at {lookAtPosition}");
        }
    }
}
