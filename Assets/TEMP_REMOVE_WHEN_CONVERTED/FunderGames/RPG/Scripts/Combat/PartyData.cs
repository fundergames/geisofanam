using System.Collections.Generic;
using UnityEngine;

namespace FunderGames.RPG
{
    [CreateAssetMenu(fileName = "Party Data", menuName = "FunderGames/Party Data")]
    public class PartyData : ScriptableObject
    {
        private List<HeroStats> partyMembers = new();
        public IReadOnlyCollection<HeroStats> PartyMembers => partyMembers;

        // Add a hero to the party by creating a new HeroStats from HeroData
        public void AddHero(HeroData heroData)
        {
            if (ContainsHero(heroData)) return;
            var newHeroStats = new HeroStats(heroData); // Initialize HeroStats with the HeroData
            partyMembers.Add(newHeroStats);
        }

        // Remove a hero from the party
        public void RemoveHero(HeroData heroData)
        {
            var heroToRemove = partyMembers.Find(h => h.HeroData == heroData);
            if (heroToRemove != null)
            {
                partyMembers.Remove(heroToRemove);
            }
        }

        // Check if the hero is already in the party (by HeroData reference)
        public bool ContainsHero(HeroData heroData)
        {
            return partyMembers.Exists(h => h.HeroData == heroData);
        }

        // Get hero by index
        public HeroStats GetHeroAt(int index)
        {
            if (index >= 0 && index < partyMembers.Count)
            {
                return partyMembers[index];
            }
            return null;
        }
    }
}