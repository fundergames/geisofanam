using Funder.Core.Randoms;
using UnityEngine;

namespace RogueDeal.Combat
{
    [CreateAssetMenu(fileName = "PokerHand_", menuName = "Funder Games/Rogue Deal/Combat/Poker Hand Definition")]
    public class PokerHandDefinition : ScriptableObject
    {
        [Header("Hand Info")]
        public PokerHandType handType;
        public string displayName;
        [TextArea(2, 3)]
        public string description;
        
        [Header("Damage")]
        public DamageRange damageRange;
        
        [Header("Class-Specific Overrides")]
        public bool allowClassOverrides = true;

        public int RollDamage(IRandomStream stream)
        {
            return damageRange.RollDamage(stream);
        }
    }
}
