using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace FunderGames.Core
{
    public static class AsyncUtility
    {
        public static async Task<T> ExecuteWithRetriesAsync<T>(
            Func<Task<T>> operation,
            int maxRetries = 3,
            int initialRetryDelay = 1000,
            Action<Exception, int> onError = null,
            CancellationToken? cancellationToken = default)
        {
            var attempt = 0;
            var retryDelay = initialRetryDelay;

            while (attempt <= maxRetries)
            {
                attempt++;
                try
                {
                    if (cancellationToken != null) cancellationToken.Value.ThrowIfCancellationRequested();
                    return await operation();
                }
                catch (Exception ex)
                {
                    if (cancellationToken != null && cancellationToken.Value.IsCancellationRequested)
                    {
                        Debug.LogWarning($"Operation cancelled on attempt {attempt}: {ex.Message}");
                        throw new TaskCanceledException("Operation was cancelled.", ex, cancellationToken.Value);
                    }

                    Debug.LogError($"Attempt {attempt} failed: {ex.Message}");
                    onError?.Invoke(ex, attempt);

                    if (attempt > maxRetries)
                    {
                        Debug.LogError($"Operation failed after {maxRetries} retries.");
                        break;
                    }

                    // Apply exponential backoff with jitter
                    retryDelay = (int)(retryDelay * UnityEngine.Random.Range(0.8f, 1.2f));
                    retryDelay = Math.Min(retryDelay, 10000); // Cap delay at 10 seconds
                    if (cancellationToken != null) await Task.Delay(retryDelay, cancellationToken.Value);
                    retryDelay *= 2; // Exponential backoff
                }
            }

            return default;
        }
    }
}