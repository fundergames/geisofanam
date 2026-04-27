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
#pragma warning disable CS0618 // OnOpenAsset still provides int; use EntityIdToObject when migrating to Unity 6+ EntityId
            Object obj = EditorUtility.InstanceIDToObject(instanceID);
#pragma warning restore CS0618
            
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
