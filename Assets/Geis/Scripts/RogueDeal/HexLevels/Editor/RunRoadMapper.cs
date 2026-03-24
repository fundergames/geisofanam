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
        [MenuItem("Funder Games/Hex Levels/Run Road Mapper Now")]
        public static void RunNow()
        {
            RoadConnectionMapper.ApplyRoadMappings();
        }
    }
}
