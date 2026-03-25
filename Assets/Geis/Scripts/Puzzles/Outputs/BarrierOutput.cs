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

        private void Awake()
        {
            if (barrierRenderer != null)
            {
                _mat           = barrierRenderer.material;
                _originalColor = _mat.color;
            }
        }

        protected override void OnActivate()
        {
            if (_mat == null) return;

            _mat.DOKill();
            _mat.DOFade(0f, fadeDuration).SetEase(ease).OnComplete(() =>
            {
                if (barrierRenderer != null)
                    barrierRenderer.enabled = false;
                if (barrierCollider != null)
                    barrierCollider.enabled = false;
            });
        }

        protected override void OnDeactivate()
        {
            if (_mat == null) return;

            if (barrierRenderer != null) barrierRenderer.enabled = true;
            if (barrierCollider != null) barrierCollider.enabled = true;

            _mat.DOKill();
            _mat.DOFade(_originalColor.a, fadeDuration).SetEase(ease);
        }
    }
}
