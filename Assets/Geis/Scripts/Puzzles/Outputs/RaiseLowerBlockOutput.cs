using DG.Tweening;
using UnityEngine;

namespace Geis.Puzzles
{
    /// <summary>
    /// Translates a block from its original position to a raised/lowered offset when activated.
    /// </summary>
    public class RaiseLowerBlockOutput : PuzzleOutputBase
    {
        [Header("Block")]
        [SerializeField] private Transform block;
        [Tooltip("Offset added to the block's original world position when activated.")]
        [SerializeField] private Vector3 raisedOffset = new Vector3(0f, 3f, 0f);
        [SerializeField] private float   duration     = 1.2f;
        [SerializeField] private Ease    ease         = Ease.InOutBack;

        private Vector3 _basePosition;

        private void Start()
        {
            if (block != null)
                _basePosition = block.position;
        }

        protected override void OnActivate()
        {
            if (block == null) return;
            block.DOKill();
            block.DOMove(_basePosition + raisedOffset, duration).SetEase(ease);
        }

        protected override void OnDeactivate()
        {
            if (block == null) return;
            block.DOKill();
            block.DOMove(_basePosition, duration).SetEase(ease);
        }
    }
}
