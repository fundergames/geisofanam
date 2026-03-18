using Funder.Core.Services;
using UnityEngine;

namespace Funder.GameFlow
{
    [CreateAssetMenu(fileName = "ProductBootstrap", menuName = "Funder Games/Core/Product Bootstrap", order = 0)]
    public class ProductBootstrap : ScriptableObject
    {
        [Header("Product Configuration")]
        [Tooltip("Unique identifier for this product (e.g., 'RogueDeal', 'MyOtherGame')")]
        public string productId = "RogueDeal";

        [Tooltip("Display name for this product")]
        public string productName = "Rogue Deal";

        [Header("App Config")]
        [Tooltip("Direct reference to the FGAppConfig for this product")]
        public FGAppConfig appConfig;

        [Header("Optional Settings")]
        [Tooltip("If true, this product will be auto-detected on startup")]
        public bool autoDetect = true;

        public void Initialize()
        {
            Debug.Log($"[ProductBootstrap] Initializing product: {productName} (ID: {productId})");
            
            if (appConfig != null)
            {
                FGConfigManager.SetConfig(appConfig);
            }
            else
            {
                Debug.LogWarning($"[ProductBootstrap] No config assigned to {productName}!");
            }
        }
    }
}
