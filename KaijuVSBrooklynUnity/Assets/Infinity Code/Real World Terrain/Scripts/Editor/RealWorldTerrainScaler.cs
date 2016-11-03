/*     INFINITY CODE 2013-2015      */
/*   http://www.infinity-code.com   */

using UnityEditor;
using UnityEngine;

public class RealWorldTerrainScaler : EditorWindow
{
    private static RealWorldTerrainScaler wnd;
    private RealWorldTerrainContainer item;
    private Vector3 scale = Vector3.one;
    private bool scaleFirstSP = true;

// ReSharper disable once UnusedMember.Local
    private void OnGUI()
    {
        scale = EditorGUILayout.Vector3Field("Scale", scale);
        scaleFirstSP = EditorGUILayout.Toggle("Scale first SplatPrototype", scaleFirstSP);
        if (GUILayout.Button("Apply")) Scale();
    }

    public static void OpenWindow(RealWorldTerrainContainer item)
    {
        wnd = GetWindow<RealWorldTerrainScaler>("Scaler", true);
        wnd.item = item;
    }

    private void Scale()
    {
        if (scale.x == 0 || scale.y == 0 || scale.z == 0)
        {
            Debug.LogError("Scale failed!!! Value can not be zero.");
            return;
        }
        if (scale.x < 0 || scale.y < 0 || scale.z < 0)
        {
            Debug.LogError("Scale failed!!! Value can not be lower zero.");
            return;
        }
        foreach (RealWorldTerrainItem terrain in item.terrains)
        {
            Vector3 p = terrain.transform.position;
            p.Scale(scale);
            terrain.transform.position = p;

            Vector3 s = terrain.terrainData.size;
            s.Scale(scale);
            terrain.terrainData.size = s;

            if (scaleFirstSP && terrain.terrainData.splatPrototypes.Length > 0)
            {
                SplatPrototype[] sps = terrain.terrainData.splatPrototypes;
                SplatPrototype sp = sps[0];
                sp.tileSize = new Vector2(sp.tileSize.x * scale.x, sp.tileSize.y * scale.z);
                sp.tileOffset = new Vector2(sp.tileOffset.x * scale.x, sp.tileOffset.y * scale.z);
                sp.texture = sp.texture;
                terrain.terrainData.splatPrototypes = sps;
            }
        }
        Close();
    }
}