using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace FunderGames.Core
{
    public static class UnityWebRequestUtility
    {
        /// <summary>
        /// Global configuration for UnityWebRequest retries.
        /// </summary>
        public static class Config
        {
            public static int MaxRetries { get; set; } = 3;
            public static int InitialRetryDelay { get; set; } = 1000; // in milliseconds
            public static int Timeout { get; set; } = 30; // in seconds
            public static Func<UnityWebRequest, bool> ShouldRetry { get; set; } = DefaultRetryCondition;
        }

        /// <summary>
        /// Metrics for tracking request statistics.
        /// </summary>
        public static class Metrics
        {
            public static int TotalRequests { get; private set; }
            public static int TotalRetries { get; private set; }
            public static int TotalFailures { get; private set; }

            public static void Reset()
            {
                TotalRequests = 0;
                TotalRetries = 0;
                TotalFailures = 0;
            }

            internal static void IncrementRequest() => TotalRequests++;
            internal static void IncrementRetry() => TotalRetries++;
            internal static void IncrementFailure() => TotalFailures++;
        }

        /// <summary>
        /// Executes a UnityWebRequest with retry logic, error handling, and optional exponential backoff.
        /// </summary>
        public static async Task<UnityWebRequest> ExecuteWithRetriesAsync(
        Func<UnityWebRequest> createRequest,
        Action<string, int> onError = null,
        int? overrideMaxRetries = null,
        int? overrideInitialRetryDelay = null,
        int? overrideTimeout = null,
        CancellationToken? cancellationToken = null)
        {
            return await SafeExecutionUtility.ExecuteAsync(
                async () =>
                {
                    Metrics.IncrementRequest();

                    var maxRetries = overrideMaxRetries ?? Config.MaxRetries;
                    var retryDelay = overrideInitialRetryDelay ?? Config.InitialRetryDelay;
                    var timeout = overrideTimeout ?? Config.Timeout;

                    var attempt = 0;

                    while (attempt <= maxRetries)
                    {
                        attempt++;
                        var request = createRequest();

                        if (timeout > 0)
                            request.timeout = timeout;

                        try
                        {
                            var operation = request.SendWebRequest();
                            while (!operation.isDone)
                            {
                                cancellationToken?.ThrowIfCancellationRequested();
                                await Task.Yield();
                            }

                            if (request.result == UnityWebRequest.Result.Success || !Config.ShouldRetry(request))
                            {
                                return request;
                            }

                            throw new Exception($"Error: {request.error}, URL: {request.url}, Status Code: {request.responseCode}");
                        }
                        catch (Exception ex)
                        {
                            onError?.Invoke(ex.Message, attempt);

                            if (attempt > maxRetries)
                            {
                                Debug.LogError($"Request failed after {maxRetries} retries: {ex.Message}");
                                Metrics.IncrementFailure();
                                break;
                            }

                            Metrics.IncrementRetry();
                            Debug.LogWarning($"Retrying ({attempt}/{maxRetries}) after error: {ex.Message}");
                            await Task.Delay((int)(retryDelay * UnityEngine.Random.Range(0.8f, 1.2f)));
                            retryDelay *= 2;
                        }
                    }

                    return null;
                },
                errorMessage: "Error executing UnityWebRequest",
                onError: ex => Debug.LogWarning($"Detailed stack trace: {ex}")
            );
        }

        /// <summary>
        /// Perform a GET request.
        /// </summary>
        public static Task<UnityWebRequest> GetAsync(string url, int? overrideTimeout = null, CancellationToken? cancellationToken = null)
        {
            return ExecuteWithRetriesAsync(
                () => UnityWebRequest.Get(url),
                overrideTimeout: overrideTimeout,
                cancellationToken: cancellationToken
            );
        }

        /// <summary>
        /// Perform a POST JSON request.
        /// </summary>
        public static Task<UnityWebRequest> PostJsonAsync(string url, string jsonPayload, int? overrideTimeout = null, CancellationToken? cancellationToken = null)
        {
            return ExecuteWithRetriesAsync(
                () =>
                {
                    var request = UnityWebRequest.Put(url, jsonPayload);
                    request.method = UnityWebRequest.kHttpVerbPOST;
                    request.SetRequestHeader("Content-Type", "application/json");
                    return request;
                },
                overrideTimeout: overrideTimeout,
                cancellationToken: cancellationToken
            );
        }

        /// <summary>
        /// Upload a file.
        /// </summary>
        public static Task<UnityWebRequest> UploadFileAsync(string url, string filePath, int? overrideTimeout = null, CancellationToken? cancellationToken = null)
        {
            return ExecuteWithRetriesAsync(
                () =>
                {
                    if (!File.Exists(filePath))
                        throw new FileNotFoundException($"File not found: {filePath}");

                    var request = UnityWebRequest.Put(url, File.ReadAllBytes(filePath));
                    request.SetRequestHeader("Content-Type", "application/octet-stream");
                    return request;
                },
                overrideTimeout: overrideTimeout,
                cancellationToken: cancellationToken
            );
        }

        /// <summary>
        /// Download a file and save it locally.
        /// </summary>
        public static async Task<bool> DownloadFileAsync(string url, string savePath, int? overrideTimeout = null, CancellationToken? cancellationToken = null)
        {
            var request = await ExecuteWithRetriesAsync(
                () => UnityWebRequest.Get(url),
                overrideTimeout: overrideTimeout,
                cancellationToken: cancellationToken
            );

            if (request is not { result: UnityWebRequest.Result.Success })
                return false;

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(savePath) ?? string.Empty);
                await File.WriteAllBytesAsync(savePath, request.downloadHandler.data, cancellationToken ?? CancellationToken.None);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save downloaded file: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Default retry condition based on request result and status code.
        /// </summary>
        private static bool DefaultRetryCondition(UnityWebRequest request)
        {
            // Retry on connection and protocol errors
            if (request.result is UnityWebRequest.Result.ConnectionError or UnityWebRequest.Result.ProtocolError)
            {
                return true;
            }

            // Retry on specific status codes (e.g., 5xx server errors)
            return request.responseCode is >= 500 and < 600;
        }
    }
}
