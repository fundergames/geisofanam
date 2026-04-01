using DG.Tweening;
using UnityEngine;

namespace Geis.Puzzles
{
    /// <summary>
    /// Dissolves a barrier (fades renderer alpha to 0 and disables collider) on activate,
    /// restores it on deactivate.
    /// </summary>
    public class BarrierOutput : PuzzleOutputBase
    {
        [Header("Barrier")]
        [SerializeField] private Renderer  barrierRenderer;
        [SerializeField] private Collider  barrierCollider;
        [SerializeField] private float     fadeDuration = 0.8f;
        [SerializeField] private Ease      ease         = Ease.InOutQuad;

        private Material _mat;
        private Color    _originalColor;
        private int      _mainColorPropId = -1;

        private static int ResolveMainColorPropertyId(Material mat)
        {
            if (mat == null) return -1;
            if (mat.HasProperty("_BaseColor")) return Shader.PropertyToID("_BaseColor");
            if (mat.HasProperty("_Color")) return Shader.PropertyToID("_Color");
            return -1;
        }

        private void Awake()
        {
            if (barrierRenderer == null) return;
            // In edit mode, .material creates a leaked instance; use shared for cache. Play mode uses an instance for DOTween.
            _mat = Application.isPlaying ? barrierRenderer.material : barrierRenderer.sharedMaterial;
            _mainColorPropId = ResolveMainColorPropertyId(_mat);
            if (_mainColorPropId >= 0)
                _originalColor = _mat.GetColor(_mainColorPropId);
            else
                _originalColor = Color.white;
        }

        protected override void OnActivate()
        {
            if (!Application.isPlaying) return;
            if (_mat == null) return;

            _mat.DOKill();

            void CompleteFade()
            {
                if (barrierRenderer != null)
                    barrierRenderer.enabled = false;
                if (barrierCollider != null)
                    barrierCollider.enabled = false;
            }

            if (_mainColorPropId >= 0)
            {
                var end = new Color(_originalColor.r, _originalColor.g, _originalColor.b, 0f);
                DOTween.To(() => _mat.GetColor(_mainColorPropId), c => _mat.SetColor(_mainColorPropId, c), end, fadeDuration)
                    .SetEase(ease)
                    .SetTarget(_mat)
                    .OnComplete(CompleteFade);
            }
            else
            {
                DOVirtual.DelayedCall(fadeDuration, CompleteFade).SetTarget(this);
            }
        }

        protected override void OnDeactivate()
        {
            if (!Application.isPlaying) return;
            if (_mat == null) return;

            if (barrierRenderer != null) barrierRenderer.enabled = true;
            if (barrierCollider != null) barrierCollider.enabled = true;

            _mat.DOKill();
            DOTween.Kill(this);

            if (_mainColorPropId < 0) return;

            DOTween.To(() => _mat.GetColor(_mainColorPropId), c => _mat.SetColor(_mainColorPropId, c), _originalColor, fadeDuration)
                .SetEase(ease)
                .SetTarget(_mat);
        }
    }
}
