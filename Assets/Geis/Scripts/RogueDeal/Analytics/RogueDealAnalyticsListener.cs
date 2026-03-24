using System;
using Funder.Core.Analytics;
using Funder.Core.Services;
using Funder.Core.Events;
using Funder.GameFlow.Events;

namespace RogueDeal.Analytics
{
    public class RogueDealAnalyticsListener : IAnalyticsEventListener, IInitializable, IDisposable
    {
        protected IEventBus _eventBus;
        protected IAnalyticsService _analytics;
        
        private IDisposable _sceneTransitionToken;
        private IDisposable _panelOpenedToken;
        private IDisposable _panelClosedToken;
        private IDisposable _loadingScreenToken;

        public void Initialize()
        {
            if (GameBootstrap.ServiceLocator != null)
            {
                _eventBus = GameBootstrap.ServiceLocator.Resolve<IEventBus>();
                _analytics = GameBootstrap.ServiceLocator.Resolve<IAnalyticsService>();
                SubscribeToGameEvents();
            }
        }

        protected virtual void SubscribeToGameEvents()
        {
            _sceneTransitionToken = _eventBus.Subscribe<SceneTransitionEvent>(OnSceneTransition);
            _panelOpenedToken = _eventBus.Subscribe<PanelOpenedEvent>(OnPanelOpened);
            _panelClosedToken = _eventBus.Subscribe<PanelClosedEvent>(OnPanelClosed);
            _loadingScreenToken = _eventBus.Subscribe<LoadingScreenEvent>(OnLoadingScreen);
        }

        private void OnSceneTransition(SceneTransitionEvent evt)
        {
            _analytics.LogEvent(
                "screen_view",
                ("screen_name", evt.ToScene),
                ("previous_screen", evt.FromScene),
                ("trigger", evt.Trigger),
                ("load_time_seconds", evt.LoadTime)
            );
        }

        private void OnPanelOpened(PanelOpenedEvent evt)
        {
            _analytics.LogEvent(
                "panel_opened",
                ("panel_name", evt.PanelName),
                ("panel_mode", evt.PanelMode),
                ("source", evt.Source)
            );
        }

        private void OnPanelClosed(PanelClosedEvent evt)
        {
            _analytics.LogEvent(
                "panel_closed",
                ("panel_name", evt.PanelName),
                ("time_open_seconds", evt.TimeOpen)
            );
        }

        private void OnLoadingScreen(LoadingScreenEvent evt)
        {
            _analytics.LogEvent(
                "loading_screen",
                ("action", evt.Action),
                ("message", evt.Message),
                ("target_scene", evt.TargetScene)
            );
        }

        public virtual void Dispose()
        {
            _sceneTransitionToken?.Dispose();
            _panelOpenedToken?.Dispose();
            _panelClosedToken?.Dispose();
            _loadingScreenToken?.Dispose();
        }
    }
}
