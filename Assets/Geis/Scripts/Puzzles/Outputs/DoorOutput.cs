using DG.Tweening;
using UnityEngine;

namespace Geis.Puzzles
{
    /// <summary>
    /// Opens/closes a door by translating or rotating it.
    /// Supports both offset-based (slide) and angle-based (swing) doors.
    /// </summary>
    public class DoorOutput : PuzzleOutputBase
    {
        public enum DoorMode { Slide, Swing }

        [Header("Door")]
        [SerializeField] private Transform door;
        [SerializeField] private DoorMode  mode = DoorMode.Swing;

        [Header("Swing")]
        [Tooltip("Euler angle added in local space when open.")]
        [SerializeField] private Vector3 openRotationOffset = new Vector3(0f, 90f, 0f);

        [Header("Slide")]
        [Tooltip("World-space offset when open.")]
        [SerializeField] private Vector3 openPositionOffset = new Vector3(0f, 3f, 0f);

        [Header("Animation")]
        [SerializeField] private float duration = 1f;
        [SerializeField] private Ease  ease     = Ease.InOutQuad;

        private Vector3    _closedPosition;
        private Quaternion _closedRotation;

        private void Start()
        {
            if (door == null) return;
            _closedPosition = door.position;
            _closedRotation = door.localRotation;
        }

        protected override void OnActivate()
        {
            if (door == null) return;
            door.DOKill();

            if (mode == DoorMode.Swing)
                door.DOLocalRotate(_closedRotation.eulerAngles + openRotationOffset, duration).SetEase(ease);
            else
                door.DOMove(_closedPosition + openPositionOffset, duration).SetEase(ease);
        }

        protected override void OnDeactivate()
        {
            if (door == null) return;
            door.DOKill();

            if (mode == DoorMode.Swing)
                door.DOLocalRotateQuaternion(_closedRotation, duration).SetEase(ease);
            else
                door.DOMove(_closedPosition, duration).SetEase(ease);
        }
    }
}
