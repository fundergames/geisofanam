using UnityEngine;

namespace Funder.Core.Flow
{
    public class FGAppConfig : ScriptableObject
    {
        public string entrySceneName = "Entry";
        public string loginSceneName = "Login";
        public string menuSceneName = "MainMenu";
        public string gameSceneName = "Game";
        public string resultsSceneName = "Results";

        public bool skipLoginInEditor = true;
        public bool skipLoginInBuilds = false;
        public bool skipMainMenuInEditor = true;
        public bool showLoadingCanvas = true;
        public bool initializeFirebaseOnStart = false;

        public int environment = 0;
        public string environmentId = "dev";

        public string EntrySceneName => entrySceneName;
        public string LoginSceneName => loginSceneName;
        public string MenuSceneName => menuSceneName;
        public string GameSceneName => gameSceneName;
        public string ResultsSceneName => resultsSceneName;
        public bool ShowLoadingCanvas => showLoadingCanvas;
        public bool SkipLoginInEditor => skipLoginInEditor;
        public bool SkipMainMenuInEditor => skipMainMenuInEditor;
        public bool SkipLoginInBuilds => skipLoginInBuilds;
    }
}
