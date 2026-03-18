using Funder.Core.Events;
using RogueDeal.NPCs;

namespace RogueDeal.Events
{
    public struct DialogStartedEvent : IEvent
    {
        public string npcId;
        public string dialogTreeId;
    }

    public struct DialogEndedEvent : IEvent
    {
        public string npcId;
    }

    public struct DialogNodeShownEvent : IEvent
    {
        public DialogNode node;
        public string npcId;
    }

    public struct DialogChoiceSelectedEvent : IEvent
    {
        public string nodeId;
        public int choiceIndex;
        public string choiceText;
    }
}