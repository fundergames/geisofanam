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

        public RandomHubService(RandomConfig config)
        {
            if (config != null)
            {
                var seed = config.GetRuntimeSeed();
                _implementation = new RandomHub(seed);
                Debug.Log($"[RandomHubService] Created with config seed: {seed}");
            }
        }

        public void Initialize()
        {
            if (_implementation == null)
            {
                const uint DEFAULT_SEED = 12345u;
                _implementation = new RandomHub(DEFAULT_SEED);
                Debug.Log($"[RandomHubService] Initialized with default seed: {DEFAULT_SEED}");
            }
        }

        public uint RootSeed => _implementation.RootSeed;

        public IRandomStream GetStream(string name)
        {
            return _implementation.GetStream(name);
        }

        public void Reseed(uint newRootSeed)
        {
            _implementation.Reseed(newRootSeed);
        }

        public void ResetAll()
        {
            _implementation.ResetAll();
        }

        public void BeginRecording(string label)
        {
            _implementation.BeginRecording(label);
        }

        public RandomReplay StopRecording()
        {
            return _implementation.StopRecording();
        }

        public void Play(RandomReplay replay)
        {
            _implementation.Play(replay);
        }
    }
}
