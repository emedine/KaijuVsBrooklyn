/*     INFINITY CODE 2013-2015      */
/*   http://www.infinity-code.com   */

using UnityEditor;
using UnityEngine;

public class RealWorldTerrainAboutWindow:EditorWindow
{
    [MenuItem("Window/Infinity Code/Real World Terrain/About")]
    public static void OpenWindow()
    {
        RealWorldTerrainAboutWindow window = GetWindow<RealWorldTerrainAboutWindow>(true, "About", true);
        window.minSize = new Vector2(200, 100);
        window.maxSize = new Vector2(200, 100);
    }

    public void OnGUI()
    {
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
        titleStyle.alignment = TextAnchor.MiddleCenter;

        GUIStyle textStyle = new GUIStyle(EditorStyles.label);
        textStyle.alignment = TextAnchor.MiddleCenter;

        GUILayout.Label("Real World Terrain", titleStyle);
        GUILayout.Label("version " + RealWorldTerrain.version, textStyle);
        GUILayout.Label("created Infinity Code", textStyle);
        GUILayout.Label("2013-2015", textStyle);
    }
}