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
