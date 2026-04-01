using TMPro;
using UnityEngine;

namespace Geis.Puzzles
{
    /// <summary>
    /// Small floating letter shown when a puzzle is interactable (default "X" when no prefab is assigned).
    /// </summary>
    public static class PuzzleInteractionPrompt
    {
        public static GameObject CreateWorldLetterPrompt(Transform parent, Vector3 localOffset, string letter = "X")
        {
            var go = new GameObject("InteractPrompt");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localOffset;

            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = letter;
            tmp.fontSize = 6f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.outlineWidth = 0.25f;
            tmp.outlineColor = new Color(0f, 0f, 0f, 0.85f);

            go.AddComponent<PuzzlePromptBillboard>();
            return go;
        }
    }

    internal sealed class PuzzlePromptBillboard : MonoBehaviour
    {
        private void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null)
                return;
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        }
    }
}
