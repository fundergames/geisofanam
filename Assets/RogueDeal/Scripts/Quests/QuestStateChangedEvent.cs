using Funder.Core.Events;

namespace RogueDeal.Quests
{
    public struct QuestStateChangedEvent : IEvent
    {
        public string questId;
        public QuestStatus status;
    }
}

