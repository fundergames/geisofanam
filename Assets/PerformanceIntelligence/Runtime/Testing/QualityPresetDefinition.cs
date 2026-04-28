using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PerformanceIntelligence.Testing
{
    [CreateAssetMenu(
        fileName = "QualityPreset",
        menuName = "Performance Intelligence/Testing/Quality Preset Definition")]
    public sealed class QualityPresetDefinition : ScriptableObject
    {
        private static bool _hasResolutionSnapshot;
        private static int _snapshotWidth;
        private static int _snapshotHeight;
        private static FullScreenMode _snapshotFullScreenMode;
#if UNITY_EDITOR
        private static int _snapshotGameViewSizeIndex = -1;
#endif

        public string presetName = "Balanced";
        public int qualityLevelIndex = 0;
        public int targetFrameRate = 60;
        public int vSyncCount = 0;
        public float renderScale = 1f;
        public float shadowDistance = 50f;
        public int shadowCascades = 2;
        public int antiAliasingLevel = 2;
        public int resolutionWidth = 1920;
        public int resolutionHeight = 1080;
        [TextArea(2, 4)] public string notes;

        public void Apply()
        {
            EnsureResolutionSnapshot();

            if (qualityLevelIndex >= 0 && qualityLevelIndex < QualitySettings.names.Length)
            {
                QualitySettings.SetQualityLevel(qualityLevelIndex, true);
            }
            else
            {
                Debug.LogWarning($"[PerformanceIntelligence] Invalid quality level index: {qualityLevelIndex}.");
            }

            Application.targetFrameRate = targetFrameRate;
            QualitySettings.vSyncCount = Mathf.Max(0, vSyncCount);
            QualitySettings.shadowDistance = shadowDistance;
            QualitySettings.shadowCascades = Mathf.Max(0, shadowCascades);
            QualitySettings.antiAliasing = Mathf.Max(0, antiAliasingLevel);

            if (resolutionWidth > 0 && resolutionHeight > 0)
            {
                Screen.SetResolution(resolutionWidth, resolutionHeight, Screen.fullScreenMode);
                TryApplyEditorGameViewResolution(resolutionWidth, resolutionHeight);
            }

            ApplyRenderScaleSafe(renderScale);
        }

        public static void RestoreResolutionSnapshot()
        {
            if (!_hasResolutionSnapshot) return;

            try
            {
                if (_snapshotWidth > 0 && _snapshotHeight > 0)
                {
                    Screen.SetResolution(_snapshotWidth, _snapshotHeight, _snapshotFullScreenMode);
                }
                RestoreEditorGameViewResolution();
            }
            finally
            {
                _hasResolutionSnapshot = false;
            }
        }

        private static void ApplyRenderScaleSafe(float value)
        {
            var rp = GraphicsSettings.currentRenderPipeline;
            if (rp == null)
            {
                Debug.Log("[PerformanceIntelligence] Built-in pipeline detected; renderScale setting unavailable.");
                return;
            }

            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var renderScaleProperty = rp.GetType().GetProperty("renderScale", flags);
            if (renderScaleProperty == null || !renderScaleProperty.CanWrite)
            {
                Debug.Log($"[PerformanceIntelligence] Render scale API unavailable on pipeline {rp.GetType().Name}.");
                return;
            }

            try
            {
                renderScaleProperty.SetValue(rp, value, null);
            }
            catch
            {
                Debug.LogWarning($"[PerformanceIntelligence] Failed to apply renderScale on {rp.GetType().Name}.");
            }
        }

        private static void EnsureResolutionSnapshot()
        {
            if (_hasResolutionSnapshot) return;

            _snapshotWidth = Screen.width;
            _snapshotHeight = Screen.height;
            _snapshotFullScreenMode = Screen.fullScreenMode;
            _hasResolutionSnapshot = true;
            CacheEditorGameViewResolution();
        }

        private static void TryApplyEditorGameViewResolution(int width, int height)
        {
#if UNITY_EDITOR
            if (!Application.isEditor) return;

            try
            {
                var gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
                if (gameViewType == null) return;

                var gameView = EditorWindow.GetWindow(gameViewType);
                if (gameView == null) return;

                var selectedSizeIndexProp = gameViewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var sizeCountMethod = gameViewType.GetMethod("GetDisplayViewSizeCount", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var getSizeMethod = gameViewType.GetMethod("GetDisplayViewSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (selectedSizeIndexProp == null || sizeCountMethod == null || getSizeMethod == null) return;

                int count = Convert.ToInt32(sizeCountMethod.Invoke(gameView, null));
                if (count <= 0) return;

                int matchedIndex = -1;
                for (int i = 0; i < count; i++)
                {
                    var sizeObj = getSizeMethod.Invoke(gameView, new object[] { i });
                    if (sizeObj == null) continue;
                    string label = sizeObj.ToString();
                    if (!string.IsNullOrEmpty(label) && label.Contains($"{width}x{height}"))
                    {
                        matchedIndex = i;
                        break;
                    }
                }

                if (matchedIndex >= 0)
                {
                    selectedSizeIndexProp.SetValue(gameView, matchedIndex, null);
                    gameView.Repaint();
                }
                else
                {
                    Debug.Log($"[PerformanceIntelligence] Requested {width}x{height} not found in GameView size list; keeping current editor GameView resolution.");
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"[PerformanceIntelligence] Could not apply GameView resolution in editor: {ex.Message}");
            }
#endif
        }

        private static void CacheEditorGameViewResolution()
        {
#if UNITY_EDITOR
            if (!Application.isEditor) return;
            try
            {
                var gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
                if (gameViewType == null) return;
                var gameView = EditorWindow.GetWindow(gameViewType);
                if (gameView == null) return;
                var selectedSizeIndexProp = gameViewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (selectedSizeIndexProp == null) return;
                _snapshotGameViewSizeIndex = Convert.ToInt32(selectedSizeIndexProp.GetValue(gameView, null));
            }
            catch
            {
                _snapshotGameViewSizeIndex = -1;
            }
#endif
        }

        private static void RestoreEditorGameViewResolution()
        {
#if UNITY_EDITOR
            if (!Application.isEditor) return;
            if (_snapshotGameViewSizeIndex < 0) return;

            try
            {
                var gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
                if (gameViewType == null) return;
                var gameView = EditorWindow.GetWindow(gameViewType);
                if (gameView == null) return;
                var selectedSizeIndexProp = gameViewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (selectedSizeIndexProp == null) return;
                selectedSizeIndexProp.SetValue(gameView, _snapshotGameViewSizeIndex, null);
                gameView.Repaint();
            }
            catch (Exception ex)
            {
                Debug.Log($"[PerformanceIntelligence] Could not restore GameView resolution in editor: {ex.Message}");
            }
            finally
            {
                _snapshotGameViewSizeIndex = -1;
            }
#endif
        }
    }
}
