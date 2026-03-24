using System.Collections.Generic;
using Funder.Core.Services;

namespace RogueDeal.Quests
{
    public interface IQuestService : IService
    {
        bool TryStartQuest(string questId);
        bool TryFailQuest(string questId);
        bool TryAbandonQuest(string questId);

        bool IsQuestCompleted(string questId);
        bool IsQuestActive(string questId);

        IReadOnlyList<QuestProgress> GetAllProgress();
        bool TryGetProgress(string questId, out QuestProgress progress);

        void Save();
        void Load();
        void ClearAllProgress();
    }
}

