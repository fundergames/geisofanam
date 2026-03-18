using UnityEngine;

namespace RogueDeal.UI
{
    [CreateAssetMenu(fileName = "CardLayoutConfig", menuName = "Funder Games/Rogue Deal/UI/Card Layout Config")]
    public class CardLayoutConfig : ScriptableObject
    {
        [Header("Layout Settings")]
        public LayoutType layoutType = LayoutType.Arc;
        
        [Header("Position")]
        public Vector2 centerPosition = new Vector2(0f, -300f);
        public float cardSpacing = 150f;
        
        [Header("Arc Layout")]
        [Range(0f, 180f)]
        public float arcAngle = 30f;
        public float arcRadius = 800f;
        public float arcVerticalOffset = -100f;
        
        [Header("Linear Layout")]
        public float linearYOffset = 0f;
        
        [Header("Fan Layout")]
        [Range(-45f, 45f)]
        public float fanRotationRange = 20f;
        
        [Header("Animation Timing")]
        public float dealDuration = 0.5f;
        public float dealDelay = 0.1f;
        public float flipDuration = 0.3f;
        public float replaceDuration = 0.5f;
        public float highlightDuration = 0.4f;
        
        [Header("Card Deck Position")]
        public Vector2 deckPosition = new Vector2(800f, 400f);
        public Vector2 discardPosition = new Vector2(-800f, 400f);

        public Vector3 GetCardPosition(int cardIndex, int totalCards)
        {
            return layoutType switch
            {
                LayoutType.Linear => GetLinearPosition(cardIndex, totalCards),
                LayoutType.Arc => GetArcPosition(cardIndex, totalCards),
                LayoutType.Fan => GetFanPosition(cardIndex, totalCards),
                _ => GetLinearPosition(cardIndex, totalCards)
            };
        }

        public Quaternion GetCardRotation(int cardIndex, int totalCards)
        {
            if (layoutType == LayoutType.Arc)
            {
                float normalizedIndex = (totalCards > 1) ? (float)cardIndex / (totalCards - 1) : 0.5f;
                float angleInRadians = Mathf.Deg2Rad * Mathf.Lerp(-arcAngle / 2f, arcAngle / 2f, normalizedIndex);
                float rotationAngle = -angleInRadians * Mathf.Rad2Deg;
                return Quaternion.Euler(0f, 0f, rotationAngle);
            }
            else if (layoutType == LayoutType.Fan)
            {
                float normalizedIndex = (totalCards > 1) ? (float)cardIndex / (totalCards - 1) : 0.5f;
                float rotation = Mathf.Lerp(-fanRotationRange, fanRotationRange, normalizedIndex);
                return Quaternion.Euler(0f, 0f, rotation);
            }
            
            return Quaternion.identity;
        }

        private Vector3 GetLinearPosition(int cardIndex, int totalCards)
        {
            float totalWidth = (totalCards - 1) * cardSpacing;
            float startX = centerPosition.x - (totalWidth / 2f);
            
            return new Vector3(
                startX + (cardIndex * cardSpacing),
                centerPosition.y + linearYOffset,
                0f
            );
        }

        private Vector3 GetArcPosition(int cardIndex, int totalCards)
        {
            float normalizedIndex = (totalCards > 1) ? (float)cardIndex / (totalCards - 1) : 0.5f;
            float angleInRadians = Mathf.Deg2Rad * Mathf.Lerp(-arcAngle / 2f, arcAngle / 2f, normalizedIndex);
            
            float x = centerPosition.x + Mathf.Sin(angleInRadians) * arcRadius;
            float y = centerPosition.y + arcVerticalOffset + Mathf.Cos(angleInRadians) * arcRadius - arcRadius;
            
            return new Vector3(x, y, 0f);
        }

        private Vector3 GetFanPosition(int cardIndex, int totalCards)
        {
            float totalWidth = (totalCards - 1) * cardSpacing;
            float startX = centerPosition.x - (totalWidth / 2f);
            
            float normalizedIndex = (totalCards > 1) ? (float)cardIndex / (totalCards - 1) : 0.5f;
            float yOffset = -Mathf.Abs(normalizedIndex - 0.5f) * 50f;
            
            return new Vector3(
                startX + (cardIndex * cardSpacing),
                centerPosition.y + yOffset,
                0f
            );
        }
    }

    public enum LayoutType
    {
        Linear,
        Arc,
        Fan
    }
}
