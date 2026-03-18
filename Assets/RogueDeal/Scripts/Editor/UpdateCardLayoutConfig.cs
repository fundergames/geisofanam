using UnityEngine;
using UnityEditor;
using RogueDeal.UI;

namespace RogueDeal.Editor
{
    public class UpdateCardLayoutConfig : EditorWindow
    {
        [MenuItem("Rogue Deal/Match Card Layout to CardArea")]
        public static void MatchCardLayoutToCardArea()
        {
            string assetPath = "Assets/RogueDeal/Resources/Configs/CardLayoutConfig.asset";
            CardLayoutConfig config = AssetDatabase.LoadAssetAtPath<CardLayoutConfig>(assetPath);
            
            if (config != null)
            {
                config.layoutType = LayoutType.Arc;
                config.centerPosition = new Vector2(0f, 0f);
                config.arcRadius = 1200f;
                config.arcAngle = 32.5f;
                config.arcVerticalOffset = -50f;
                config.deckPosition = new Vector2(400f, 150f);
                config.discardPosition = new Vector2(-400f, 150f);
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
                Debug.Log("Updated CardLayoutConfig to match CardArea: Arc layout with radius=1200, angle=32.5°, offset=-50");
            }
            else
            {
                Debug.LogError($"Could not find CardLayoutConfig at {assetPath}");
            }
            
            string gameplayAssetPath = "Assets/RogueDeal/Resources/Configs/Gameplay/CardLayoutConfig.asset";
            CardLayoutConfig gameplayConfig = AssetDatabase.LoadAssetAtPath<CardLayoutConfig>(gameplayAssetPath);
            
            if (gameplayConfig != null)
            {
                gameplayConfig.layoutType = LayoutType.Arc;
                gameplayConfig.centerPosition = new Vector2(0f, 0f);
                gameplayConfig.arcRadius = 1200f;
                gameplayConfig.arcAngle = 32.5f;
                gameplayConfig.arcVerticalOffset = -50f;
                gameplayConfig.deckPosition = new Vector2(400f, 150f);
                gameplayConfig.discardPosition = new Vector2(-400f, 150f);
                EditorUtility.SetDirty(gameplayConfig);
                AssetDatabase.SaveAssets();
                Debug.Log("Updated Gameplay CardLayoutConfig to match CardArea: Arc layout with radius=1200, angle=32.5°, offset=-50");
            }
            
            FixCardContainerPosition();
        }
        
        private static void FixCardContainerPosition()
        {
            GameObject canvasObj = UnityEngine.GameObject.Find("Canvas");
            if (canvasObj == null)
            {
                Debug.LogError("Could not find Canvas in the scene");
                return;
            }
            
            Transform cardAreaTransform = canvasObj.transform.Find("CardArea");
            Transform cardHandTransform = canvasObj.transform.Find("CardHand");
            
            if (cardAreaTransform == null)
            {
                Debug.LogError("Could not find CardArea");
                return;
            }
            
            if (cardHandTransform == null)
            {
                Debug.LogError("Could not find CardHand");
                return;
            }
            
            RectTransform cardAreaRect = cardAreaTransform.GetComponent<RectTransform>();
            RectTransform cardHandRect = cardHandTransform.GetComponent<RectTransform>();
            
            if (cardAreaRect != null && cardHandRect != null)
            {
                cardHandRect.anchorMin = cardAreaRect.anchorMin;
                cardHandRect.anchorMax = cardAreaRect.anchorMax;
                cardHandRect.pivot = cardAreaRect.pivot;
                cardHandRect.anchoredPosition = cardAreaRect.anchoredPosition;
                cardHandRect.sizeDelta = cardAreaRect.sizeDelta;
                
                UnityEditor.EditorUtility.SetDirty(cardHandRect);
                Debug.Log($"Matched CardHand to CardArea - Anchors: {cardAreaRect.anchorMin} to {cardAreaRect.anchorMax}, Pivot: {cardAreaRect.pivot}");
            }
            
            Transform cardContainerTransform = cardHandTransform.Find("CardContainer");
            if (cardContainerTransform != null)
            {
                RectTransform rectTransform = cardContainerTransform.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchorMin = new UnityEngine.Vector2(0.5f, 0.5f);
                    rectTransform.anchorMax = new UnityEngine.Vector2(0.5f, 0.5f);
                    rectTransform.pivot = new UnityEngine.Vector2(0.5f, 0.5f);
                    rectTransform.anchoredPosition = UnityEngine.Vector2.zero;
                    rectTransform.sizeDelta = new UnityEngine.Vector2(1000, 300);
                    rectTransform.localPosition = UnityEngine.Vector3.zero;
                    
                    UnityEditor.EditorUtility.SetDirty(rectTransform);
                    Debug.Log("Fixed CardContainer - Anchors: (0.5, 0.5), Position: (0, 0, 0)");
                }
            }
            else
            {
                Debug.LogError("Could not find CardContainer");
            }
        }
    }
}
