using UnityEngine;

namespace Geis.SoulRealm.WeaponAbilities
{
    /// <summary>
    /// Runtime world ring + optional vertical marker so marked targets stay readable in soul and physical realm
    /// without assigning a VFX prefab.
    /// </summary>
    internal sealed class SoulMarkProceduralIndicator : MonoBehaviour
    {
        private const int RingSegments = 48;

        private static Material _sharedUnlitSprite;

        private static Material SharedUnlitSpriteMaterial
        {
            get
            {
                if (_sharedUnlitSprite == null)
                {
                    var sh = Shader.Find("Sprites/Default");
                    if (sh != null)
                        _sharedUnlitSprite = new Material(sh);
                }
                return _sharedUnlitSprite;
            }
        }

        private LineRenderer _ring;
        private LineRenderer _cross;
        private Color _colorSoul;
        private Color _colorPhysical;
        private float _pulsePhase;

        public static SoulMarkProceduralIndicator Create(
            Transform parent,
            Vector3 localOffset,
            float ringRadius,
            Color colorSoul,
            Color colorPhysical)
        {
            var go = new GameObject("SoulMarkIndicator");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localOffset;

            var ind = go.AddComponent<SoulMarkProceduralIndicator>();
            ind._colorSoul = colorSoul;
            ind._colorPhysical = colorPhysical;
            ind.BuildRing(ringRadius);
            ind.BuildCross(ringRadius);
            return ind;
        }

        private void BuildRing(float radius)
        {
            _ring = gameObject.AddComponent<LineRenderer>();
            _ring.loop = true;
            _ring.positionCount = RingSegments;
            _ring.widthMultiplier = 0.035f;
            _ring.useWorldSpace = false;
            _ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _ring.receiveShadows = false;

            if (SharedUnlitSpriteMaterial != null)
                _ring.material = SharedUnlitSpriteMaterial;

            float step = Mathf.PI * 2f / RingSegments;
            for (int i = 0; i < RingSegments; i++)
            {
                float a = step * i;
                _ring.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
            }
        }

        private void BuildCross(float radius)
        {
            var child = new GameObject("SoulMarkCross");
            child.transform.SetParent(transform, false);

            _cross = child.AddComponent<LineRenderer>();
            _cross.loop = false;
            _cross.positionCount = 5;
            _cross.widthMultiplier = 0.03f;
            _cross.useWorldSpace = false;
            _cross.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _cross.receiveShadows = false;

            float h = radius * 1.15f;
            _cross.SetPosition(0, new Vector3(0f, 0f, -h));
            _cross.SetPosition(1, new Vector3(0f, 0f, h));
            _cross.SetPosition(2, new Vector3(0f, 0f, 0f));
            _cross.SetPosition(3, new Vector3(-h, 0f, 0f));
            _cross.SetPosition(4, new Vector3(h, 0f, 0f));

            if (SharedUnlitSpriteMaterial != null)
                _cross.material = SharedUnlitSpriteMaterial;
        }

        private void Update()
        {
            float blend = 0f;
            if (SoulRealmManager.Instance != null && SoulRealmManager.Instance.IsSoulRealmActive)
                blend = 1f;

            Color c = Color.Lerp(_colorPhysical, _colorSoul, blend);
            _pulsePhase += Time.deltaTime * 2.2f;
            float pulse = 0.85f + 0.15f * Mathf.Sin(_pulsePhase);
            c.a *= pulse;

            if (_ring != null)
                _ring.startColor = _ring.endColor = c;
            if (_cross != null)
                _cross.startColor = _cross.endColor = c;

            transform.Rotate(Vector3.up, 28f * Time.deltaTime, Space.Self);
        }
    }
}
