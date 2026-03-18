using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Funder.Core.Flow
{
    public static class FGFlow
    {
        public enum State
        {
            Login,
            Menu,
            Game,
            Results
        }

        public static State CurrentState { get; private set; } = State.Login;

        public static async Task GoTo(State nextState, FGAppConfig config)
        {
            if (config == null)
            {
                Debug.LogError("[FGFlow] GoTo: FGAppConfig is null.");
                return;
            }

            string sceneName = ResolveScene(nextState, config);
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError($"[FGFlow] No scene configured for state {nextState}.");
                return;
            }

            var asyncOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (asyncOp == null)
            {
                Debug.LogError($"[FGFlow] Failed to load scene '{sceneName}'.");
                return;
            }

            while (!asyncOp.isDone)
                await Task.Yield();

            CurrentState = nextState;
        }

        public static void OnLoginComplete() => CurrentState = State.Menu;
        public static void StartGame() => CurrentState = State.Game;
        public static void FinishGame() => CurrentState = State.Results;
        public static void BackToMenu() => CurrentState = State.Menu;

        private static string ResolveScene(State state, FGAppConfig config)
        {
            return state switch
            {
                State.Login => config.LoginSceneName,
                State.Menu => config.MenuSceneName,
                State.Game => config.GameSceneName,
                State.Results => config.ResultsSceneName,
                _ => null
            };
        }
    }
}
