using UnityEngine;

namespace Geis.SoulRealm.WeaponAbilities
{
    /// <summary>
    /// Default markable target. Add to props or enemy roots; optional VFX on mark.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SoulMarkTarget : MonoBehaviour, ISoulMarkable
    {
        [SerializeField] private GameObject markVfxPrefab;
        [SerializeField] private Vector3 markVfxOffset;

        private bool _marked;
        private GameObject _vfx;

        public Transform MarkTransform => transform;
        public bool IsSoulMarked => _marked;

        public void ApplySoulMark()
        {
            _marked = true;
            if (markVfxPrefab != null && _vfx == null)
                _vfx = Instantiate(markVfxPrefab, transform.position + markVfxOffset, Quaternion.identity, transform);
        }

        public void ClearSoulMark()
        {
            _marked = false;
            if (_vfx != null)
            {
                Destroy(_vfx);
                _vfx = null;
            }
        }

        private void OnDestroy()
        {
            if (_vfx != null)
                Destroy(_vfx);
        }
    }
}
