/*     INFINITY CODE 2013-2015      */
/*   http://www.infinity-code.com   */

using UnityEditor;
using UnityEngine;

public class RealWorldTerrainExtCurrentLatLonWindow : EditorWindow
{
    private RealWorldTerrainContainer rwt;
    private Vector3 lastCursorPosition;

    [MenuItem("Window/Infinity Code/Real World Terrain/Extensions/Current Position")]
    // ReSharper disable once UnusedMember.Local
    private static void OpenWindow()
    {
        RealWorldTerrainExtCurrentLatLonWindow wnd = GetWindow<RealWorldTerrainExtCurrentLatLonWindow>(false, "Current Position");
        EditorApplication.update += wnd.OnUpdate;
    }

    private void OnEnable()
    {
        EditorApplication.update += OnUpdate;
        SceneView.onSceneGUIDelegate += OnSceneGUI;
    }

    private void OnUpdate()
    {
        Repaint();
    }

    private void OnGUI()
    {
        rwt = (RealWorldTerrainContainer)EditorGUILayout.ObjectField("Real World Terrain", rwt, typeof(RealWorldTerrainContainer), true);

        if (rwt == null) return;

        float minX = float.MaxValue;
        float minZ = float.MaxValue;
        float maxX = float.MinValue;
        float maxZ = float.MinValue;

        foreach (RealWorldTerrainItem item in rwt.terrains)
        {
            if (item == null) continue;
            Bounds bounds = item.bounds;
            Vector3 p = bounds.min;
            Vector3 p2 = bounds.max;
            if (p.x < minX) minX = p.x;
            if (p.z < minZ) minZ = p.z;
            if (p2.x > maxX) maxX = p2.x;
            if (p2.z > maxZ) maxZ = p2.z;
        }

        if (minX == float.MaxValue) return;

        float rX = maxX - minX;
        float rZ = maxZ - minZ;

        float sX = rwt.area.width / rX;
        float sZ = -rwt.area.height / rZ;

        SceneView view = SceneView.lastActiveSceneView;
        if (view == null) return;

        Vector3 cp = view.camera.transform.position;
        cp.x = (cp.x - minX) * sX + rwt.area.x;
        cp.z = (cp.z - minZ) * sZ + rwt.area.y + rwt.area.height;
        
        EditorGUILayout.LabelField("Scene camera latitude: " + cp.z);
        EditorGUILayout.LabelField("Scene camera longitude: " + cp.x);

        if (lastCursorPosition == Vector3.zero) return;

        Vector3 cp2 = lastCursorPosition;
        cp2.x = (cp2.x - minX) * sX + rwt.area.x;
        cp2.z = (cp2.z - minZ) * sZ + rwt.area.y + rwt.area.height;

        EditorGUILayout.LabelField("Scene cursor latitude: " + cp2.z);
        EditorGUILayout.LabelField("Scene cursor longitude: " + cp2.x);
    }

    private void OnSceneGUI(SceneView view)
    {
        Vector2 mp = Event.current.mousePosition;
        mp.y = view.camera.pixelHeight - mp.y;

        RaycastHit hit;
        Ray ray = view.camera.ScreenPointToRay(mp);

        if (Physics.Raycast(ray.origin, ray.direction, out hit, 100000000)) lastCursorPosition = hit.point;
        else lastCursorPosition = Vector3.zero;
    }
}
