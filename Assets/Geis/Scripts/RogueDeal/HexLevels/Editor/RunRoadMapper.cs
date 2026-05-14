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

using UnityEngine;
using UnityEditor;
using RogueDeal.HexLevels.Editor;

namespace RogueDeal.HexLevels.Editor
{
    /// <summary>
    /// Helper script to run the road mapper.
    /// Use the menu item: Funder Games > Hex Levels > Apply Road Connection Mappings
    /// Or call RoadConnectionMapper.ApplyRoadMappings() directly.
    /// </summary>
    public class RunRoadMapper : EditorWindow
    {
        [MenuItem("Funder Games/Geis/Rogue Deal/Hex Levels/Run Road Mapper Now")]
        public static void RunNow()
        {
            RoadConnectionMapper.ApplyRoadMappings();
        }
    }
}
