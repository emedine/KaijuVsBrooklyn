/*     INFINITY CODE 2013-2015      */
/*   http://www.infinity-code.com   */

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RealWorldTerrainRoadArchitectNode))]
public class RealWorldTerrainRoadArchitectNodeEditor : Editor
{
#if ROADARCHITECT
    private RealWorldTerrainRoadArchitectNode node;
    private GSDSplineN splineN;

    private void OnEnable()
    {
        node = (RealWorldTerrainRoadArchitectNode) target;
        splineN = node.GetComponent<GSDSplineN>();
    }


    public override void OnInspectorGUI()
    {
        bool allowIntersect = !splineN.bNeverIntersect;
        bool newAI = EditorGUILayout.Toggle("Allow intersect: ", allowIntersect);
        if (newAI != allowIntersect)
        {
            splineN.bNeverIntersect = !newAI;
        }
    }
#endif
}