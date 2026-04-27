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

using System;
using System.Threading.Tasks;
using UnityEngine;

namespace RogueDeal.Utils
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
