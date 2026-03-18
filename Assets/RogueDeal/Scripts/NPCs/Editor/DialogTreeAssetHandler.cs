using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace RogueDeal.NPCs.Editor
{
    public class DialogTreeAssetHandler
    {
        [OnOpenAsset(1)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            Object obj = EditorUtility.InstanceIDToObject(instanceID);
            
            if (obj is DialogTree dialogTree)
            {
                OpenDialogTreeEditor(dialogTree);
                return true; // Prevent default inspector from opening
            }
            
            return false; // Let Unity handle other assets normally
        }
        
        private static void OpenDialogTreeEditor(DialogTree tree)
        {
            DialogTreeEditorWindow window = EditorWindow.GetWindow<DialogTreeEditorWindow>("Dialog Tree Editor");
            window.LoadDialogTree(tree);
            window.Focus();
        }
    }
}
