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
using Funder.Core.Services;
using Funder.GameFlow;
using System.Threading.Tasks;
using RogueDeal.Player;

namespace RogueDeal.CharacterSelect
{
    public class CharacterSelectBootstrap : MonoBehaviour
    {
        [Header("Hero Selection")]
        [SerializeField] private CharacterSelectionView selectionView;

        private async void Start()
        {
            await WaitForBootstrap();
            InitializeCharacterSelect();
        }

        private async Task WaitForBootstrap()
        {
            while (!GameBootstrap.IsInitialized)
            {
                await Task.Yield();
            }
        }

        private void InitializeCharacterSelect()
        {
            if (selectionView != null)
            {
                selectionView.Initialize();
            }
            else
            {
                Debug.LogError("[CharacterSelectBootstrap] CharacterSelectionView not assigned!");
            }
        }
    }
}
