using System.Collections.Generic;
using System.Threading.Tasks;
using FunderGames.UI;
using UnityEngine;

namespace FunderGames.Core
{
    public class InitializationManager : Singleton<InitializationManager>
    {
        public bool IsInitialized { get; private set; } = false;

        private readonly List<IInitializable> initializables = new();

        protected override async void Awake()
        {
            base.Awake();
            await InitializeSystemsAsync();
        }

        private async Task InitializeSystemsAsync()
        {
            Debug.Log("Initializing systems...");

            // Register systems
            RegisterSystems();

            // Initialize each system
            foreach (var system in initializables)
            {
                Debug.Log($"Initializing {system.GetType().Name}...");
                await system.InitializeAsync();
            }

            IsInitialized = true;
            Debug.Log("All systems initialized.");
        }

        private void RegisterSystems()
        {
            // Add systems to initialize
            initializables.Add(UIWindowManager.Instance);
            
            // Add other systems here, e.g., AudioManager, SaveSystem, etc.
        }
    }
}