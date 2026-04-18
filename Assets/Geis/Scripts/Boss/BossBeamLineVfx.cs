using UnityEngine;

namespace RogueDeal.Boss
{
    [DisallowMultipleComponent]
    public sealed class BossBeamLineVfx : MonoBehaviour
    {
        [SerializeField] private LineRenderer line;

        private float _dieAt;
        private bool _playing;

        private void Awake()
        {
            EnsureLine();
            SetVisible(false);
        }

        private void Update()
        {
            if (!_playing) return;
            if (Time.time >= _dieAt)
            {
                _playing = false;
                SetVisible(false);
                gameObject.SetActive(false);
            }
        }

        public void Play(Vector3 origin, Vector3 end, Color color, float width, float durationSeconds, Material materialOverride = null)
        {
            EnsureLine();

            if (materialOverride != null)
                line.sharedMaterial = materialOverride;

            width = Mathf.Max(0.001f, width);
            durationSeconds = Mathf.Max(0.01f, durationSeconds);

            line.startWidth = width;
            line.endWidth = width;

            line.startColor = color;
            line.endColor = color;

            line.positionCount = 2;
            line.SetPosition(0, origin);
            line.SetPosition(1, end);

            _dieAt = Time.time + durationSeconds;
            _playing = true;
            SetVisible(true);
            gameObject.SetActive(true);
        }

        private void EnsureLine()
        {
            if (line != null) return;

            line = GetComponent<LineRenderer>();
            if (line == null)
                line = gameObject.AddComponent<LineRenderer>();

            line.useWorldSpace = true;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            line.alignment = LineAlignment.View;
            line.numCornerVertices = 2;
            line.numCapVertices = 2;

            if (line.sharedMaterial == null)
                line.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        }

        private void SetVisible(bool visible)
        {
            if (line != null)
                line.enabled = visible;
        }
    }
}

