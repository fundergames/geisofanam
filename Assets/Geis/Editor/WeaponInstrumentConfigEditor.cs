// Geis of Anam - Combat Music System
// Editor for WeaponInstrumentConfig: auto-populate pentatonic clips from Musical_Instruments_And_Notes.

using UnityEditor;
using UnityEngine;

namespace Geis.Combat.Music.Editor
{
    [CustomEditor(typeof(WeaponInstrumentConfig))]
    public class WeaponInstrumentConfigEditor : UnityEditor.Editor
    {
        private const string BasePath = "Assets/Musical_Instruments_And_Notes/Wave/44_1kHz-16bit";
        private static readonly (int fileNum, string noteName)[] PentatonicFiles = 
        {
            (15, "D4"), (18, "F4"), (20, "G4"), (22, "A4"), (25, "C5")
        };

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var config = (WeaponInstrumentConfig)target;
            if (config == null) return;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Pentatonic Auto-Populate", EditorStyles.boldLabel);
            if (GUILayout.Button("Populate Pentatonic From Instrument Folder"))
            {
                PopulatePentatonic(config);
            }
        }

        [MenuItem("Geis/Combat/Music/Create Default Marimba Config")]
        private static void CreateDefaultMarimbaConfig()
        {
            const string path = "Assets/Geis/Combat/Music/Configs/WeaponInstrumentConfig_Marimba.asset";
            var config = AssetDatabase.LoadAssetAtPath<WeaponInstrumentConfig>(path);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<WeaponInstrumentConfig>();
                string dir = System.IO.Path.GetDirectoryName(path);
                if (!AssetDatabase.IsValidFolder("Assets/Geis/Combat"))
                {
                    if (!AssetDatabase.IsValidFolder("Assets/Geis")) AssetDatabase.CreateFolder("Assets", "Geis");
                    if (!AssetDatabase.IsValidFolder("Assets/Geis/Combat")) AssetDatabase.CreateFolder("Assets/Geis", "Combat");
                }
                if (!AssetDatabase.IsValidFolder("Assets/Geis/Combat/Music"))
                    AssetDatabase.CreateFolder("Assets/Geis/Combat", "Music");
                if (!AssetDatabase.IsValidFolder("Assets/Geis/Combat/Music/Configs"))
                    AssetDatabase.CreateFolder("Assets/Geis/Combat/Music", "Configs");
                AssetDatabase.CreateAsset(config, path);
            }
            PopulatePentatonic(config);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(config);
            Selection.activeObject = config;
        }

        [MenuItem("Geis/Combat/Music/Populate Selected WeaponInstrumentConfig")]
        private static void PopulateSelectedConfig()
        {
            var config = Selection.activeObject as WeaponInstrumentConfig;
            if (config == null)
            {
                Debug.LogWarning("Select a WeaponInstrumentConfig asset.");
                return;
            }
            PopulatePentatonic(config);
        }

        private static void PopulatePentatonic(WeaponInstrumentConfig config)
        {
            string folderName = config.InstrumentFolderName;
            if (string.IsNullOrEmpty(folderName))
            {
                Debug.LogWarning("Instrument folder name is empty.");
                return;
            }

            string searchFolder = $"{BasePath}/{folderName}";
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { searchFolder });
            if (guids.Length == 0)
            {
                Debug.LogWarning($"No AudioClips found in {searchFolder}. Check the folder path and Instrument Folder Name.");
                return;
            }

            var clips = new AudioClip[5];
            for (int i = 0; i < PentatonicFiles.Length; i++)
            {
                var (fileNum, noteName) = PentatonicFiles[i];
                string prefix = $"{fileNum:D2}_{noteName}_";

                AudioClip clip = null;
                foreach (string guid in guids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    string fileName = System.IO.Path.GetFileName(assetPath);
                    if (fileName.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                    {
                        clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                        break;
                    }
                }

                if (clip == null)
                    Debug.LogWarning($"Could not find clip for {prefix}* in {searchFolder}");
                clips[i] = clip;
            }

            config.SetPentatonicClips(clips);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            int found = 0;
            for (int j = 0; j < clips.Length; j++) if (clips[j] != null) found++;
            Debug.Log($"Populated {folderName}: {found}/5 clips found.");
        }
    }
}
