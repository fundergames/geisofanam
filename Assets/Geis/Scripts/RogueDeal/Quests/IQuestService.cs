/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 *
 * This software and associated documentation files are proprietary and confidential.
 * Unauthorized copying, modification, distribution, or use of this software,
 * via any medium, is strictly prohibited without explicit written permission.
 *
 * This code is provided for personal use only by authorized recipients.
 * It may not be redistributed, sublicensed, or sold in any form.
 */

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

