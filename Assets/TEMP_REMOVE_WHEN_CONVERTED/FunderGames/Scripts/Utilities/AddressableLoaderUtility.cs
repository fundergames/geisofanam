using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace FunderGames.Core
{
    public static class AddressableLoaderUtility
    {
        public static async Task<T> LoadWithRetriesAsync<T>(
            string key,
            int maxRetries = 3,
            int initialRetryDelay = 500,
            CancellationToken? cancellationToken = default)
        {
            return await AsyncUtility.ExecuteWithRetriesAsync(
                async () =>
                {
                    var handle = Addressables.LoadAssetAsync<T>(key);
                    while (!handle.IsDone)
                    {
                        if (cancellationToken != null) cancellationToken.Value.ThrowIfCancellationRequested();
                        await Task.Yield();
                    }

                    if (handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        return handle.Result;
                    }
                    else
                    {
                        throw new Exception($"Addressables failed to load asset with key '{key}': {handle.OperationException}");
                    }
                },
                maxRetries: maxRetries,
                initialRetryDelay: initialRetryDelay,
                onError: (ex, attempt) => Debug.LogError($"Attempt {attempt} to load Addressable asset failed: {ex.Message}"),
                cancellationToken: cancellationToken
            );
        }
    }
}