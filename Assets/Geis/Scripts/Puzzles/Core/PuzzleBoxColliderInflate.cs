using UnityEngine;

namespace Geis.Puzzles
{
    /// <summary>
    /// One-time capture + additive inflate for thin puzzle BoxColliders (pressure plates, sword hit zones).
    /// </summary>
    public static class PuzzleBoxColliderInflate
    {
        public static void ApplyIfNeeded(
            Collider col,
            bool inflateEnabled,
            Vector3 inflate,
            ref bool baseCaptured,
            ref Vector3 storedBaseSize,
            ref Vector3 storedBaseCenter)
        {
            if (!inflateEnabled || inflate.sqrMagnitude < 1e-8f)
                return;
            if (col is not BoxCollider box)
                return;

            if (!baseCaptured)
            {
                storedBaseSize = box.size;
                storedBaseCenter = box.center;
                baseCaptured = true;
            }

            box.size = storedBaseSize + inflate;
            box.center = storedBaseCenter + new Vector3(0f, inflate.y * 0.5f, 0f);
        }
    }
}
