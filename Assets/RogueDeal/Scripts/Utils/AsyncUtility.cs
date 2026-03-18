using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace RogueDeal.Utils
{
    public static class AsyncUtility
    {
        public static async Task<T> ExecuteWithRetriesAsync<T>(
            Func<Task<T>> action,
            int maxRetries = 3,
            int initialRetryDelay = 1000,
            Action<Exception, int> onError = null,
            CancellationToken? cancellationToken = null)
        {
            var attempt = 0;
            var retryDelay = initialRetryDelay;

            while (attempt < maxRetries)
            {
                attempt++;

                try
                {
                    if (cancellationToken?.IsCancellationRequested == true)
                    {
                        throw new OperationCanceledException("Operation was cancelled.");
                    }

                    return await action();
                }
                catch (Exception ex)
                {
                    onError?.Invoke(ex, attempt);

                    if (attempt >= maxRetries)
                    {
                        Debug.LogError($"All retry attempts exhausted. Last error: {ex.Message}");
                        throw;
                    }

                    Debug.LogWarning($"Retry attempt {attempt}/{maxRetries} after error: {ex.Message}");
                    await Task.Delay(retryDelay, cancellationToken ?? CancellationToken.None);
                    retryDelay *= 2;
                }
            }

            return default;
        }

        public static async Task ExecuteWithRetriesAsync(
            Func<Task> action,
            int maxRetries = 3,
            int initialRetryDelay = 1000,
            Action<Exception, int> onError = null,
            CancellationToken? cancellationToken = null)
        {
            var attempt = 0;
            var retryDelay = initialRetryDelay;

            while (attempt < maxRetries)
            {
                attempt++;

                try
                {
                    if (cancellationToken?.IsCancellationRequested == true)
                    {
                        throw new OperationCanceledException("Operation was cancelled.");
                    }

                    await action();
                    return;
                }
                catch (Exception ex)
                {
                    onError?.Invoke(ex, attempt);

                    if (attempt >= maxRetries)
                    {
                        Debug.LogError($"All retry attempts exhausted. Last error: {ex.Message}");
                        throw;
                    }

                    Debug.LogWarning($"Retry attempt {attempt}/{maxRetries} after error: {ex.Message}");
                    await Task.Delay(retryDelay, cancellationToken ?? CancellationToken.None);
                    retryDelay *= 2;
                }
            }
        }
    }
}
