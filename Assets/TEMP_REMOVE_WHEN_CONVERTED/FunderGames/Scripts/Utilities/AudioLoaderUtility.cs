using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace FunderGames.Core
{

    public static class AudioLoaderUtility
    {
        /// <summary>
        /// Load an audio clip asynchronously from a file path with retries, error handling, and exponential backoff.
        /// </summary>
        /// <param name="path">The full file path to the audio file.</param>
        /// <param name="cancellationToken">Optional token to cancel the operation.</param>
        /// <returns>The loaded AudioClip, or null if loading failed.</returns>
        public static async Task<AudioClip> LoadAudioClipWithRetriesAsync(string path,
            CancellationToken cancellationToken = default)
        {
            return await AsyncUtility.ExecuteWithRetriesAsync(
                async () => await LoadAudioClipWithRetriesAsync(path, cancellationToken),
                maxRetries: 5,
                initialRetryDelay: 500, // Start with a 500ms retry delay
                onError: (ex, attempt) => Debug.LogError($"Attempt {attempt} to load audio failed: {ex.Message}"),
                cancellationToken: cancellationToken
            );
        }

        /// <summary>
        /// Core method to load an audio clip asynchronously from a file path.
        /// </summary>
        /// <param name="path">The full file path to the audio file.</param>
        /// <param name="audioType">The type of audio file being requested</param>
        /// <param name="cancellationToken">Optional token to cancel the operation.</param>
        /// <param name="url">URL of request</param>
        /// <param name="maxRetries">Total number of retries before failing</param>
        /// <param name="initialRetryDelay">Time between retries</param>
        /// <returns>The loaded AudioClip, or null if loading failed.</returns>
        public static async Task<AudioClip> LoadAudioClipWithRetriesAsync(
            string url,
            AudioType audioType,
            CancellationToken cancellationToken,
            int maxRetries = 3,
            int initialRetryDelay = 1000)
        {
            return await UnityWebRequestUtility.ExecuteWithRetriesAsync(
                createRequest: () => UnityWebRequestMultimedia.GetAudioClip(url, audioType),
                onError: (error, attempt) => Debug.LogWarning($"Attempt {attempt} to load audio clip failed: {error}"),
                overrideMaxRetries: maxRetries,
                overrideInitialRetryDelay: initialRetryDelay,
                cancellationToken: cancellationToken
            ).ContinueWith(t =>
            {
                var request = t.Result;

                if (request == null)
                {
                    throw new Exception($"Failed to load audio clip after {maxRetries} retries.");
                }

                return DownloadHandlerAudioClip.GetContent(request);
            }, cancellationToken);
        }
    }
}