/*     INFINITY CODE 2013-2015      */
/*   http://www.infinity-code.com   */

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof (RealWorldTerrainExtLookLatLon))]
public class RealWorldTerrainExtLookLatLonEditor : Editor
{
    private RealWorldTerrainExtLookLatLon item;

    private void OnEnable()
    {
        item = (RealWorldTerrainExtLookLatLon) target;
    }

    public override void OnInspectorGUI()
    {
        item.lat = EditorGUILayout.FloatField("Lat: ", item.lat);
        item.lon = EditorGUILayout.FloatField("Lon: ", item.lon);

        if (GUILayout.Button("Look to")) RealWorldTerrainExtLookLatLon.LookTo(item.lon, item.lat);

        GUILayout.Space(10);
        item.distance = EditorGUILayout.FloatField("Distance: ", item.distance);
        item.height = EditorGUILayout.FloatField("Height: ", item.height);

        if (GUILayout.Button("Move to"))
            RealWorldTerrainExtLookLatLon.MoveTo(item.lon, item.lat, item.distance, item.height);
    }
}