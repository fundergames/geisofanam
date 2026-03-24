using UnityEngine;
using UnityEditor;
using System.Text;

namespace RogueDeal.HexLevels.Editor
{
    public class RoadPrototypeReference : EditorWindow
    {
        private Vector2 scrollPos;
        
        [MenuItem("Tools/Hex Levels/Road Prototype Reference")]
        public static void ShowWindow()
        {
            GetWindow<RoadPrototypeReference>("Road Prototype Reference");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Road Prototype Reference", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Connection directions: 0=E, 1=NE, 2=NW, 3=W, 4=SW, 5=SE", MessageType.Info);
            
            GUILayout.Space(10);
            
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            
            DrawPrototypeInfo("A", 9, "E+W straight");
            DrawPrototypeInfo("B", 17, "E+SW curve");
            DrawPrototypeInfo("C", 33, "E+SE curve");
            DrawPrototypeInfo("D", 21, "E+NW+SW 3-way");
            DrawPrototypeInfo("E", 25, "E+W+SW straight+curve");
            DrawPrototypeInfo("F", 13, "E+W+NW straight+curve");
            DrawPrototypeInfo("G", 35, "E+NE+SE 3-way");
            DrawPrototypeInfo("H", 41, "E+W+SE+NE 4-way");
            DrawPrototypeInfo("I", 54, "NE+NW+SW+SE 4-way no straight");
            DrawPrototypeInfo("J", 15, "E+NE+NW+W 4-way");
            DrawPrototypeInfo("K", 55, "5-way missing W");
            DrawPrototypeInfo("L", 63, "6-way all");
            DrawPrototypeInfo("M", 1, "E endcap");
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawPrototypeInfo(string variant, int basePattern, string description)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUILayout.Label($"Variant {variant} - {description}", EditorStyles.boldLabel);
            GUILayout.Label($"Base Pattern: {basePattern} (0b{System.Convert.ToString(basePattern, 2).PadLeft(6, '0')})", EditorStyles.miniLabel);
            
            GUILayout.Space(5);
            
            // Show all 6 rotations
            for (int rot = 0; rot < 6; rot++)
            {
                int rotationDegrees = rot * 60;
                int rotatedPattern = RotatePattern(basePattern, rot);
                string binary = System.Convert.ToString(rotatedPattern, 2).PadLeft(6, '0');
                string connections = GetConnectionsString(rotatedPattern);
                
                string rotationLabel = $"  {rotationDegrees,3}° → Pattern {rotatedPattern,2} (0b{binary}) = {connections}";
                GUILayout.Label(rotationLabel, EditorStyles.miniLabel);
            }
            
            GUILayout.Space(5);
            EditorGUILayout.EndVertical();
            GUILayout.Space(5);
        }
        
        private int RotatePattern(int pattern, int steps)
        {
            steps = steps % 6;
            if (steps == 0) return pattern;
            
            int result = 0;
            for (int i = 0; i < 6; i++)
            {
                if ((pattern & (1 << i)) != 0)
                {
                    // Rotate clockwise: E→SE→SW→W→NW→NE→E
                    int newPos = (i - steps + 6) % 6;
                    result |= (1 << newPos);
                }
            }
            return result;
        }
        
        private string GetConnectionsString(int pattern)
        {
            StringBuilder sb = new StringBuilder();
            string[] dirs = { "E", "NE", "NW", "W", "SW", "SE" };
            
            for (int i = 0; i < 6; i++)
            {
                if ((pattern & (1 << i)) != 0)
                {
                    if (sb.Length > 0) sb.Append("+");
                    sb.Append(dirs[i]);
                }
            }
            
            return sb.Length > 0 ? sb.ToString() : "none";
        }
    }
}
