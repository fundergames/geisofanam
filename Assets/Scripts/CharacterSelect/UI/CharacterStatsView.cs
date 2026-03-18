using UnityEngine;
using RogueDeal.Player;

namespace RogueDeal.UI
{
    public class CharacterStatsView : MonoBehaviour
    {
        [SerializeField] private GameObject sliderPrefab;

        public void UpdateDisplay(StatsData data)
        {
            Debug.Log($"[CharacterStatsView] UpdateDisplay called, data is null: {data == null}");
            
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            if (data == null || data.Stats == null) 
            {
                Debug.LogWarning("[CharacterStatsView] Data or Stats is null, aborting");
                return;
            }

            Debug.Log($"[CharacterStatsView] Creating {data.Stats.Count} stat sliders");
            
            foreach (var stat in data.Stats)
            {
                var slider = Instantiate(sliderPrefab, transform);
                var sliderComponent = slider.GetComponent<StatsSlider>();
                if (sliderComponent != null)
                {
                    Debug.Log($"[CharacterStatsView] Updating slider for {stat.DisplayText}");
                    sliderComponent.UpdateView(stat.DisplayText, stat.Icon, stat.Amount, stat.Color);
                }
                else
                {
                    Debug.LogError("[CharacterStatsView] StatsSlider component not found on prefab!");
                }
            }
        }
    }
}
