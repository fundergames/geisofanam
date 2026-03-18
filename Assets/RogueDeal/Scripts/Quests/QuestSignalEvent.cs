using Funder.Core.Events;

namespace RogueDeal.Quests
{
    /// <summary>
    /// Generic "quest signal" that gameplay can raise to advance objectives.
    ///
    /// Examples:
    /// - key="enemy_defeated", targetId="slime", amount=1
    /// - key="item_collected", targetId="potion", amount=3
    /// - key="npc_talked", targetId="blacksmith", amount=1
    /// - key="location_reached", targetId="village_gate", amount=1
    /// </summary>
    public struct QuestSignalEvent : IEvent
    {
        public string key;
        public string targetId;
        public int amount;
    }
}

