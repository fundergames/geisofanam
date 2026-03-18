using UnityEngine;
using UnityEditor;
using RogueDeal.Combat;

namespace RogueDeal.Editor
{
    public static class FixHealthBarScale
    {
        [MenuItem("RogueDeal/Fix Health Bar Scale (World Space)")]
        public static void FixAllHealthBars()
        {
            EnemyHealthBar[] healthBars = GameObject.FindObjectsByType<EnemyHealthBar>(FindObjectsSortMode.None);
            
            if (healthBars.Length == 0)
            {
                EditorUtility.DisplayDialog("Info",
                    "No EnemyHealthBar components found in the scene!",
                    "OK");
                return;
            }

            int fixedCount = 0;
            
            foreach (EnemyHealthBar healthBar in healthBars)
            {
                Canvas canvas = healthBar.GetComponent<Canvas>();
                if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
                {
                    RectTransform rectTransform = healthBar.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        if (rectTransform.localScale.x > 0.1f || 
                            rectTransform.localScale.y > 0.1f || 
                            rectTransform.localScale.z > 0.1f)
                        {
                            Undo.RecordObject(rectTransform, "Fix Health Bar Scale");
                            rectTransform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                            EditorUtility.SetDirty(healthBar.gameObject);
                            
                            Debug.Log($"Fixed scale for {healthBar.gameObject.name} → (0.01, 0.01, 0.01)");
                            fixedCount++;
                        }
                    }
                }
            }

            if (fixedCount > 0)
            {
                EditorUtility.DisplayDialog("Success!",
                    $"Fixed {fixedCount} health bar(s)!\n\n" +
                    "All World Space health bars now have correct scale (0.01, 0.01, 0.01)\n\n" +
                    "Save your scene to keep the changes.",
                    "Great!");
            }
            else
            {
                EditorUtility.DisplayDialog("Info",
                    $"Found {healthBars.Length} health bar(s), but all are already correctly scaled!",
                    "OK");
            }
        }

        [MenuItem("GameObject/RogueDeal/Fix Selected Health Bar Scale", true)]
        private static bool ValidateFixSelected()
        {
            return Selection.activeGameObject != null && 
                   Selection.activeGameObject.GetComponent<EnemyHealthBar>() != null;
        }

        [MenuItem("GameObject/RogueDeal/Fix Selected Health Bar Scale")]
        private static void FixSelectedHealthBar()
        {
            GameObject selected = Selection.activeGameObject;
            EnemyHealthBar healthBar = selected.GetComponent<EnemyHealthBar>();
            
            if (healthBar == null)
            {
                EditorUtility.DisplayDialog("Error",
                    "Selected GameObject doesn't have an EnemyHealthBar component!",
                    "OK");
                return;
            }

            Canvas canvas = healthBar.GetComponent<Canvas>();
            if (canvas == null || canvas.renderMode != RenderMode.WorldSpace)
            {
                EditorUtility.DisplayDialog("Warning",
                    "This health bar doesn't have a World Space canvas!\n\n" +
                    "Scale fix is only needed for World Space canvases.",
                    "OK");
                return;
            }

            RectTransform rectTransform = healthBar.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                Undo.RecordObject(rectTransform, "Fix Health Bar Scale");
                rectTransform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                EditorUtility.SetDirty(selected);
                
                EditorUtility.DisplayDialog("Success!",
                    $"Fixed scale for {selected.name}\n\n" +
                    "Scale set to (0.01, 0.01, 0.01)",
                    "OK");
            }
        }
    }
}
