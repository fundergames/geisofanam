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

using System;
using System.Collections.Generic;
using UnityEngine;

namespace RogueDeal.Quests
{
    public enum QuestStatus
    {
        Inactive = 0,
        Active = 1,
        Completed = 2,
        Failed = 3,
        Abandoned = 4
    }

    [Serializable]
    public class QuestProgress
    {
        public string questId;
        public QuestStatus status;
        public long startedAtUnix;
        public long completedAtUnix;
        public List<ObjectiveProgress> objectives = new List<ObjectiveProgress>();

        public bool IsTerminal =>
            status == QuestStatus.Completed ||
            status == QuestStatus.Failed ||
            status == QuestStatus.Abandoned;
    }

    [Serializable]
    public class ObjectiveProgress
    {
        public string objectiveId;
        public int currentAmount;
        public bool completed;
    }

    [Serializable]
    internal class QuestSaveData
    {
        public List<QuestProgress> quests = new List<QuestProgress>();
    }
}

