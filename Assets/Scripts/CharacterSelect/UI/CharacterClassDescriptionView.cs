using TMPro;
using UnityEngine;
using UnityEngine.UI;
using RogueDeal.Player;

namespace RogueDeal.UI
{
    public class CharacterClassDescriptionView : MonoBehaviour
    {
        [SerializeField] private Image characterClassSprite;
        [SerializeField] private TextMeshProUGUI playerName;
        [SerializeField] private TextMeshProUGUI characterClassName;
        [SerializeField] private TextMeshProUGUI description;

        public void UpdateDisplay(HeroData hero)
        {
            if (hero == null) 
            {
                Debug.LogWarning("[CharacterClassDescriptionView] Hero data is null");
                return;
            }

            Debug.Log($"[CharacterClassDescriptionView] UpdateDisplay called for {hero.PlayerName}");

            if (playerName != null)
            {
                playerName.text = hero.PlayerName;
            }

            if (characterClassSprite != null && hero.CharacterClass != null)
            {
                characterClassSprite.sprite = hero.CharacterClass.Icon;
            }

            if (characterClassName != null && hero.CharacterClass != null)
            {
                characterClassName.text = hero.CharacterClass.ClassDisplayName;
            }

            if (description != null && hero.CharacterClass != null)
            {
                description.text = hero.CharacterClass.Description;
            }
        }
    }
}
