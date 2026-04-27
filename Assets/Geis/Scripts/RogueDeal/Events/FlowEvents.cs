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

namespace Funder.GameFlow.Events
{
    public struct SceneTransitionEvent : IEvent
    {
        public string FromScene;
        public string ToScene;
        public string Trigger;
        public float LoadTime;
    }

    public struct PanelOpenedEvent : IEvent
    {
        public string PanelName;
        public string PanelMode;
        public string Source;
    }

    public struct PanelClosedEvent : IEvent
    {
        public string PanelName;
        public float TimeOpen;
    }

    public struct LoadingScreenEvent : IEvent
    {
        public string Action;
        public string Message;
        public string TargetScene;
    }
}
