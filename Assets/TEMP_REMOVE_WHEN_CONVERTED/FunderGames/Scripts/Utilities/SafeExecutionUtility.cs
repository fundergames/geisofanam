using System;
using System.Threading.Tasks;
using UnityEngine;

namespace FunderGames.Core
{
    public static class SafeExecutionUtility
    {
        public static async Task<T> ExecuteAsync<T>(
            Func<Task<T>> action,
            string errorMessage,
            Action<Exception> onError = null,
            int maxRetries = 1,
            int retryDelay = 1000)
        {
            var attempt = 0;

            while (attempt <= maxRetries)
            {
                attempt++;
                try
                {
                    return await action();
                }
                catch (Exception ex)
                {
                    onError?.Invoke(ex);

                    if (attempt > maxRetries)
                    {
                        Debug.LogError($"{errorMessage}: {ex.Message}");
                        break;
                    }

                    Debug.LogWarning($"Retrying ({attempt}/{maxRetries}) after error: {ex.Message}");
                    await Task.Delay(retryDelay);
                }
            }

            return default;
        }

        public static T Execute<T>(
            Func<T> action,
            string errorMessage,
            Action<Exception> onError = null)
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                Debug.LogError($"{errorMessage}: {ex.Message}");
                onError?.Invoke(ex);
                return default;
            }
        }
    }
}