/*     INFINITY CODE 2013-2015      */
/*   http://www.infinity-code.com   */

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof (RealWorldTerrainContainer))]
// ReSharper disable UnusedMember.Global
public class RealWorldTerrainContainerEditor : Editor
// ReSharper restore UnusedMember.Global
{
    private RealWorldTerrainContainer container;

// ReSharper disable once UnusedMember.Local
    private void CreatePrefab()
    {
        try
        {
            Transform housesTransform = container.transform.FindChild("Buildings/Houses");
            if (housesTransform != null)
            {
                string buildingsFolder = container.folder + "/Buildings";

                if (!Directory.Exists(buildingsFolder)) Directory.CreateDirectory(buildingsFolder);

                int houseCount = housesTransform.childCount;
                for (int i = 0; i < houseCount; i++)
                {
                    if (EditorUtility.DisplayCancelableProgressBar("Create Prefab", "Save house " + (i + 1) + " from " + houseCount,
                        i / (float)houseCount))
                    {
                        EditorUtility.ClearProgressBar();
                        return;
                    }

                    Transform houseTransform = housesTransform.GetChild(i);
                    Transform roofTransform = houseTransform.Find("Roof");
                    Transform wallTransform = houseTransform.Find("Wall");

                    string houseID = houseTransform.name;
                    string housePath = buildingsFolder + "/" + houseID;

                    if (!Directory.Exists(housePath)) Directory.CreateDirectory(housePath);

                    if (roofTransform != null) SaveBuildingPart(roofTransform, housePath, "Roof");
                    if (wallTransform != null) SaveBuildingPart(wallTransform, housePath, "Wall");
                }
            }

            EditorUtility.DisplayCancelableProgressBar("Create Prefab", "Save Prefab", 1);

            string containerName = container.name;
            GameObject prefab = PrefabUtility.CreatePrefab(container.folder + "/" + containerName + ".prefab", container.gameObject);
            DestroyImmediate(container.gameObject);
            container = ((GameObject)PrefabUtility.InstantiatePrefab(prefab)).GetComponent<RealWorldTerrainContainer>();
            container.name = containerName;

            EditorGUIUtility.PingObject(prefab);
        }
        catch (Exception exception)
        {
            Debug.LogError(exception.Message + "\n" + exception.StackTrace);
        }

        EditorUtility.ClearProgressBar();
    }

    private void DrawItemScale(float sizeX, float sizeY)
    {
        float minX = float.MaxValue;
        float minZ = float.MaxValue;
        float maxX = float.MinValue;
        float maxZ = float.MinValue;

        foreach (RealWorldTerrainItem item in container.terrains)
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

        GUILayout.Label("Scale X: " + sizeX * 1000 / (maxX - minX) + " meter/unit");
        GUILayout.Label("Scale Z: " + sizeY * 1000 / (maxZ - minZ) + " meter/unit");
    }

    private void DrawItemSize(Vector2 cFrom, Vector2 cTo, Vector2 range, out float sizeX, out float sizeY)
    {
        sizeX = sizeY = 0;

        if (container.sizeType == 0)
        {
            const float R = 6371;
            double scfY = Math.Sin(cFrom.y * Mathf.Deg2Rad);
            double sctY = Math.Sin(cTo.y * Mathf.Deg2Rad);
            double ccfY = Math.Cos(cFrom.y * Mathf.Deg2Rad);
            double cctY = Math.Cos(cTo.y * Mathf.Deg2Rad);
            double cX = Math.Cos(range.x * Mathf.Deg2Rad);
            double sizeX1 = Math.Abs(R * Math.Acos(scfY * scfY + ccfY * ccfY * cX));
            double sizeX2 = Math.Abs(R * Math.Acos(sctY * sctY + cctY * cctY * cX));
            sizeX = (float) ((sizeX1 + sizeX2) / 2.0);
            sizeY = (float) (R * Math.Acos(scfY * sctY + ccfY * cctY));
        }
        else if (container.sizeType == 1)
        {
            const int eqv = 40075;
            sizeX = Mathf.Abs(range.x / 360 * eqv);
            sizeY = Mathf.Abs(range.y / 360 * eqv);
        }

        GUILayout.Label("Size X: " + sizeX + " km");
        GUILayout.Label("Size Z: " + sizeY + " km");
        EditorGUILayout.Space();
    }

    private void DrawTerrainButtons()
    {
        if (GUILayout.Button("Regenerate ..."))
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("terrains"), false,
                () => RealWorldTerrain.OpenWindow(RealWorldTerrainGenerateType.terrain, container));
            menu.AddItem(new GUIContent("textures"), false,
                () => RealWorldTerrain.OpenWindow(RealWorldTerrainGenerateType.texture, container));
            menu.AddItem(new GUIContent("additional"), false,
                () => RealWorldTerrain.OpenWindow(RealWorldTerrainGenerateType.additional, container));
            menu.ShowAsContext();
        }

        if (GUILayout.Button("Scale")) RealWorldTerrainScaler.OpenWindow(container);
        if (GUILayout.Button("Export heightmaps"))
        {
            string path = EditorUtility.OpenFolderPanel("Export heightmaps", Application.dataPath, "");
            if (!path.IsNullOrEmpty())
            {
                foreach (RealWorldTerrainItem item in container.terrains)
                    RealWorldTerrainItemEditor.Export(item, Path.Combine(path, item.name + ".png"));
            }
        }
    }

    private void DrawTreeAndGrassProps()
    {
        bool needRedrawScene = false;

        EditorGUI.BeginChangeCheck();
        container.detailDistance = EditorGUILayout.Slider("Detail Distance", container.detailDistance, 0, 250);
        if (EditorGUI.EndChangeCheck())
        {
            foreach (RealWorldTerrainItem item in container.terrains)
                item.terrain.detailObjectDistance = container.detailDistance;
            needRedrawScene = true;
        }

        EditorGUI.BeginChangeCheck();
        container.detailDensity = EditorGUILayout.Slider("Detail Density", container.detailDensity, 0, 1);
        if (EditorGUI.EndChangeCheck())
        {
            foreach (RealWorldTerrainItem item in container.terrains)
                item.terrain.detailObjectDensity = container.detailDensity;
            needRedrawScene = true;
        }

        EditorGUI.BeginChangeCheck();
        container.treeDistance = EditorGUILayout.Slider("Tree Distance", container.treeDistance, 0, 2000);
        if (EditorGUI.EndChangeCheck())
        {
            foreach (RealWorldTerrainItem item in container.terrains) item.terrain.treeDistance = container.treeDistance;
            needRedrawScene = true;
        }

        EditorGUI.BeginChangeCheck();
        container.billboardStart = EditorGUILayout.Slider("Billboard Start", container.billboardStart, 5, 2000);
        if (EditorGUI.EndChangeCheck())
        {
            foreach (RealWorldTerrainItem item in container.terrains) item.terrain.treeBillboardDistance = container.billboardStart;
            needRedrawScene = true;
        }

        if (needRedrawScene) SceneView.RepaintAll();
    }

    private void OnEnable()
    {
        container = (RealWorldTerrainContainer) target;
    }

    public override void OnInspectorGUI()
    {
        RealWorldTerrainItemEditor.DrawLocationInfo(container);

        Vector2 cFrom = new Vector2(container.area.x, container.area.y);
        Vector2 cTo = new Vector2(container.area.xMax, container.area.yMax);
        Vector2 range = cFrom - cTo;

        float sizeX, sizeY;
        DrawItemSize(cFrom, cTo, range, out sizeX, out sizeY);
        DrawItemScale(sizeX, sizeY);

        DrawTreeAndGrassProps();

        if (container.resultType == RealWorldTerrainResultType.terrain) DrawTerrainButtons();
        if (GUILayout.Button("Create Prefab")) CreatePrefab();
        if (GUILayout.Button("Open Real World Terrain with current settings")) RealWorldTerrain.OpenWindow(RealWorldTerrainGenerateType.full, container);
    }

    private static void SaveBuildingPart(Transform roofTransform, string housePath, string partName)
    {
        MeshFilter meshFilter = roofTransform.GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            if (!AssetDatabase.Contains(meshFilter.sharedMesh))
            {
                string path = housePath + "/" + partName + ".asset";
                AssetDatabase.CreateAsset(meshFilter.sharedMesh, path);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                meshFilter.sharedMesh = AssetDatabase.LoadAssetAtPath(path, typeof(Mesh)) as Mesh;
            }
        }

        Renderer renderer = roofTransform.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (!AssetDatabase.Contains(renderer.sharedMaterial))
            {
                string path = housePath + "/" + partName + ".mat";
                AssetDatabase.CreateAsset(renderer.sharedMaterial, path);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath(path, typeof(Material)) as Material;
            }
        }
    }
}