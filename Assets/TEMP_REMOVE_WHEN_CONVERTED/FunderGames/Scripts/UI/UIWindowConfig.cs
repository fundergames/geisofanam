using System.Collections.Generic;
using UnityEngine;

namespace FunderGames.UI
{
    [CreateAssetMenu(fileName = "UIWindowConfig", menuName = "FunderGames/UI/WindowConfig")]
    public class UIWindowConfig : ScriptableObject
    {
        [System.Serializable]
        public class WindowMapping
        {
            public string id;
            public GameObject windowPrefab;
            public TransitionType transitionType = TransitionType.None;
            public float transitionDuration = 0.5f;
            public Vector2 slideOffset = new(1000, 0); // Default slide direction
        }

        public List<WindowMapping> windowMappings;
    }
}