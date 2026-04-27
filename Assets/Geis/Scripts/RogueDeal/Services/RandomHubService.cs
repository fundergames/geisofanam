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

using Funder.Core.Randoms;
using Funder.Core.Services;
using UnityEngine;

namespace RogueDeal.Services
{
    public class RandomHubService : IRandomHub, IInitializable
    {
        private RandomHub _implementation;

        public RandomHubService()
        {
        }

        public uint Seed => _implementation != null ? _implementation.Seed : 0;

        public void Initialize()
        {
            if (_implementation == null)
            {
                const uint DEFAULT_SEED = 12345u;
                _implementation = new RandomHub(DEFAULT_SEED);
                Debug.Log($"[RandomHubService] Initialized with default seed: {DEFAULT_SEED}");
            }
        }

        public IRandomStream GetStream(string name)
        {
            EnsureInitialized();
            return _implementation.GetStream(name);
        }

        public void Reseed(uint seed)
        {
            EnsureInitialized();
            _implementation.Reseed(seed);
        }

        private void EnsureInitialized()
        {
            if (_implementation == null)
            {
                Initialize();
            }
        }
    }
}
