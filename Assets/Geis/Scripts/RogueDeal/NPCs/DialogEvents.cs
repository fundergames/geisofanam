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