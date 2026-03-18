using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

namespace FunderGames.Core
{
    public class AssetLoadingManager : Singleton<AssetLoadingManager>
    {
        private readonly Dictionary<string, object> assetCache = new();
        /// <summary>
        /// Load an asset of any type from the specified source.
        /// </summary>
        /// <typeparam name="T">The type of the asset to load.</typeparam>
        /// <param name="key">The key or path to the asset (or null if using an AssetReference).</param>
        /// <param name="assetReference">The AssetReference to load, if applicable.</param>
        /// <param name="source">The source of the asset (Addressable, Resources, FilePath, AssetReference, SpriteAtlas).</param>
        /// <param name="useCache">Whether to cache the loaded asset for future use.</param>
        /// <param name="spriteName">Optional name of the sprite if loading from a SpriteAtlas.</param>
        /// <param name="instantiate">Instantiate prefabs immediately after loading (for Prefabs).</param>
        /// <param name="maxRetries">The maximum number of retry attempts (for Addressables or FilePath sources).</param>
        /// <param name="initialRetryDelay">The initial delay (in milliseconds) between retries.</param>
        /// <param name="cancellationToken">Optional token to cancel the operation.</param>
        /// <returns>The loaded asset of type T, or null if loading fails.</returns>
        public async Task<T> LoadAssetAsync<T>(
            string key,
            AssetSource source = AssetSource.Addressable,
            bool useCache = true,
            string spriteName = null,
            int maxRetries = 3,
            int initialRetryDelay = 1000,
            CancellationToken? cancellationToken = null) where T : Object
        {
            return await SafeExecutionUtility.ExecuteAsync(
                async () =>
                {
                    using var cts = cancellationToken?.CanBeCanceled == true ? null : new CancellationTokenSource();
                    var token = cancellationToken ?? cts.Token;

                    if (useCache && assetCache.TryGetValue(key, out var cachedAsset))
                    {
                        return (T)cachedAsset;
                    }

                    var asset = source switch
                    {
                        AssetSource.Addressable => await LoadFromAddressables<T>(key, maxRetries, initialRetryDelay, token),
                        // AssetSource.AssetReference => await LoadFromAssetReference<T>()
                        AssetSource.Resources => LoadFromResources<T>(key),
                        AssetSource.FilePath => await LoadFromFilePath<T>(key, maxRetries, initialRetryDelay, token),
                        AssetSource.SpriteAtlas => LoadFromSpriteAtlas<T>(key, spriteName),
                        _ => throw new ArgumentException($"Unsupported asset source: {source}")
                    };

                    if (useCache && asset != null)
                    {
                        assetCache[key] = asset;
                    }

                    return asset;
                },
                errorMessage: $"Error loading asset with key: {key}",
                onError: ex => Debug.LogWarning($"Detailed stack trace: {ex}"),
                maxRetries: maxRetries,
                retryDelay: initialRetryDelay
            );
        }

        private static async Task<T> LoadFromAddressables<T>(string key, int maxRetries = 3, int initialRetryDelay = 1000, CancellationToken? token = null)
        {
            return await AddressableLoaderUtility.LoadWithRetriesAsync<T>(
                key,
                maxRetries: maxRetries,
                initialRetryDelay: initialRetryDelay,
                cancellationToken: token
            );
        }

        public static async Task<T> LoadFromAssetReference<T>(AssetReference assetReference, int maxRetries = 3, int initialRetryDelay = 1000, CancellationToken? token = null) where T : UnityEngine.Object
        {
            return await AsyncUtility.ExecuteWithRetriesAsync(
                async () =>
                {
                    var handle = assetReference.LoadAssetAsync<T>();
                    while (!handle.IsDone)
                    {
                        if (token != null) token.Value.ThrowIfCancellationRequested();
                        await Task.Yield();
                    }

                    if (handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        return handle.Result;
                    }
                    else
                    {
                        throw new Exception($"Failed to load AssetReference with key '{assetReference.RuntimeKey}': {handle.OperationException}");
                    }
                },
                maxRetries: maxRetries,
                initialRetryDelay: initialRetryDelay,
                onError: (ex, attempt) => Debug.LogError($"Attempt {attempt} to load AssetReference failed: {ex.Message}"),
                cancellationToken: token
            );
        }

        private static T LoadFromResources<T>(string key) where T : Object
        {
            var asset = Resources.Load<T>(key);
            if (asset == null)
            {
                Debug.LogError($"Failed to load asset from Resources: {key}");
            }
            return asset;
        }

        private static async Task<T> LoadFromFilePath<T>(string key, int maxRetries, int initialRetryDelay, CancellationToken token) where T : Object
        {
            return await AsyncUtility.ExecuteWithRetriesAsync(
                async () =>
                {
                    var fullPath = Path.Combine(Application.dataPath, key);
                    if (!File.Exists(fullPath))
                    {
                        throw new FileNotFoundException($"File not found at path: {fullPath}");
                    }
                    
                    return typeof(T) switch
                    {
                        { } t when t == typeof(Texture2D) =>
                            await LoadTextureFromFileAsync(fullPath, token) as T,

                        { } t when t == typeof(TextAsset) =>
                            await LoadTextAssetFromFileAsync(fullPath, token) as T,

                        { } t when t == typeof(AudioClip) =>
                            await AudioLoaderUtility.LoadAudioClipWithRetriesAsync(fullPath, token) as T,

                        { } t when t == typeof(Material) =>
                            await LoadMaterialFromFileAsync(fullPath, token) as T,

                        { } t when t == typeof(TextAsset) && fullPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase) =>
                            await LoadJsonFromFileAsync(fullPath, token) as T,

                        _ => throw new NotSupportedException($"Unsupported file type: {typeof(T).Name}")
                    };
                },
                maxRetries: maxRetries,
                initialRetryDelay: initialRetryDelay,
                onError: (ex, attempt) => Debug.LogError($"Attempt {attempt} to load file asset failed: {ex.Message}"),
                cancellationToken: token
            );
        }

        private static T LoadFromSpriteAtlas<T>(string key, string spriteName) where T : Object
        {
            if (typeof(T) != typeof(Sprite))
            {
                throw new ArgumentException("SpriteAtlas loading supports only Sprite type.");
            }

            var atlas = Resources.Load<SpriteAtlas>(key);
            if (atlas == null)
            {
                Debug.LogError($"Failed to load SpriteAtlas from Resources: {key}");
                return null;
            }

            var sprite = atlas.GetSprite(spriteName);
            if (sprite != null) return sprite as T;
            Debug.LogError($"Failed to find sprite '{spriteName}' in SpriteAtlas: {key}");
            return null;

        }

        public void ClearAssetCache(string key)
        {
            if (!assetCache.ContainsKey(key)) return;
            assetCache.Remove(key);
            Addressables.Release(key);
            Debug.Log($"Asset with key '{key}' cleared from cache.");
        }

        public void ClearAllCache()
        {
            foreach (var key in assetCache.Keys)
            {
                Addressables.Release(key);
            }

            assetCache.Clear();
            Debug.Log("All assets cleared from cache.");
        }
        
        private static async Task<Texture2D> LoadTextureFromFileAsync(string path, CancellationToken token)
        {
            var fileData = await File.ReadAllBytesAsync(path, token);
            var texture = new Texture2D(2, 2);
            texture.LoadImage(fileData);
            return texture;
        }

        private static async Task<TextAsset> LoadTextAssetFromFileAsync(string path, CancellationToken token)
        {
            var textContent = await File.ReadAllTextAsync(path, token);
            return new TextAsset(textContent);
        }
        
        private static async Task<Material> LoadMaterialFromFileAsync(string path, CancellationToken token)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Material file not found: {path}");

            var fileData = await File.ReadAllBytesAsync(path, token);
            var material = new Material(Shader.Find("Standard")); // Replace with your default shader
            // Assume material data is stored in a compatible format
            // Additional parsing logic may be required for custom formats
            return material;
        }
        
        private static async Task<TextAsset> LoadJsonFromFileAsync(string path, CancellationToken token)
        {
            var jsonContent = await File.ReadAllTextAsync(path, token);
            return new TextAsset(jsonContent);
        }
    }

    public enum AssetSource
    {
        Addressable,
        AssetReference,
        Resources,
        FilePath,
        SpriteAtlas
    }
}
