/*     INFINITY CODE 2013-2015      */
/*   http://www.infinity-code.com   */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

[CustomEditor(typeof (RealWorldTerrainOSMBuilding))]
public class RealWorldTerrainOSMBuildingEditor : Editor
{
    public static GameObject baseContainer;
    public static GameObject houseContainer;
    
    private static Material defHouseRoofMaterial;
    private static Material defHouseWallMaterial;
    private RealWorldTerrainOSMBuilding building;

    public static List<RealWorldTerrainOSMNode> nodes;
    public static List<RealWorldTerrainOSMWay> ways;
    public static List<RealWorldTerrainOSMRelation> relations;
    public static bool loaded;

    private static string url
    {
        get
        {
            string format = string.Format("node({0},{1},{2},{3});way(bn)['building'];(._;>;);out;", prefs.coordinatesTo.y, prefs.coordinatesFrom.x, prefs.coordinatesFrom.y, prefs.coordinatesTo.x);
            string data = format;
            return RealWorldTerrainUtils.overpassAPI + data;
        }
    }

    private static RealWorldTerrainPrefs prefs
    {
        get { return RealWorldTerrain.prefs; }
    }

    public static string filename
    {
        get
        {
            return Path.Combine(RealWorldTerrainEditorUtils.osmCacheFolder, string.Format("buildings_{0}_{1}_{2}_{3}.osm", prefs.coordinatesTo.y, prefs.coordinatesFrom.x, prefs.coordinatesFrom.y, prefs.coordinatesTo.x));
        }
    }

    public static string compressedFilename
    {
        get
        {
            return filename + "c";
        }
    }

    private static void AddHouseWallVerticle(List<Vector3> vertices, Vector3 p, float topPoint)
    {
        Vector3 tv = new Vector3(p.x, topPoint, p.z);

        vertices.Add(p);
        vertices.Add(tv);
    }

    private static void AnalizeHouseRoofType(RealWorldTerrainOSMWay way, ref float baseHeight,
        ref RealWorldTerrainOSMRoofType roofType, ref float roofHeight)
    {
        string roofShape = way.GetTagValue("roof:shape");
        string roofHeightStr = way.GetTagValue("roof:height");
        string minHeightStr = way.GetTagValue("min_height");
        if (!String.IsNullOrEmpty(roofShape))
        {
            if ((roofShape == "dome" || roofShape == "pyramidal") && !String.IsNullOrEmpty(roofHeightStr))
            {
                GetHeightFromString(roofHeightStr, ref roofHeight);
                baseHeight -= roofHeight;
                roofType = RealWorldTerrainOSMRoofType.dome;
            }
        }
        else if (!String.IsNullOrEmpty(roofHeightStr))
        {
            GetHeightFromString(roofHeightStr, ref roofHeight);
            baseHeight -= roofHeight;
            roofType = RealWorldTerrainOSMRoofType.dome;
        }
        else if (!String.IsNullOrEmpty(minHeightStr))
        {
            float totalHeight = baseHeight;
            GetHeightFromString(minHeightStr, ref baseHeight);
            roofHeight = totalHeight - baseHeight;
            roofType = RealWorldTerrainOSMRoofType.dome;
        }
    }

    private static void AnalizeHouseTags(RealWorldTerrainOSMWay way, ref Material wallMaterial, ref Material roofMaterial, ref float baseHeight, bool useDefaultMaterials)
    {
        string heightStr = way.GetTagValue("height");
        string levelsStr = way.GetTagValue("building:levels");
        GetHeightFromString(heightStr, ref baseHeight);
        if (String.IsNullOrEmpty(heightStr) && !String.IsNullOrEmpty(levelsStr))
            baseHeight = float.Parse(levelsStr) * 3.5f;
        else baseHeight = RealWorldTerrain.prefs.buildingLevelLimits.Random() * 3.5f;

        string colorStr = way.GetTagValue("building:colour");
        if (useDefaultMaterials && !String.IsNullOrEmpty(colorStr))
            wallMaterial.color = roofMaterial.color = RealWorldTerrainUtils.StringToColor(colorStr);
    }

    private static void CreateHouse(RealWorldTerrainOSMWay way, RealWorldTerrainContainer globalContainer)
    {
        //if (way.id != "309710701")return;

        List<Vector3> points = RealWorldTerrainOSM.GetGlobalPointsFromWay(way, nodes);
        if (points.Count < 3) return;
        if (points.First() == points.Last())
        {
            points.Remove(points.Last());
            if (points.Count < 3) return;
        }

        for (int i = 0; i < points.Count; i++)
        {
            Vector3 p = RealWorldTerrainEditorUtils.GlobalToLocalWithElevation(points[i], globalContainer.scale);
            p.y = RealWorldTerrainEditorUtils.NormalizeElevation(globalContainer, p.y);
            points[i] = p;
        }

        for (int i = 0; i < points.Count; i++)
        {
            int prev = i - 1;
            if (prev < 0) prev = points.Count - 1;

            int next = i + 1;
            if (next >= points.Count) next = 0;

            if ((points[prev] - points[i]).magnitude < 1f)
            {
                points.RemoveAt(i);
                i--;
                continue;
            }

            if ((points[next] - points[i]).magnitude < 1f)
            {
                points.RemoveAt(next);
                continue;
            }

            float a1 = RealWorldTerrainUtils.Angle2D(points[prev], points[i]);
            float a2 = RealWorldTerrainUtils.Angle2D(points[i], points[next]);

            if (Mathf.Abs(a1 - a2) < 5)
            {
                points.RemoveAt(i);
                i--;
            }
        }

        if (points.Count < 3) return;

        Vector3 centerPoint = Vector3.zero;
        centerPoint = points.Aggregate(centerPoint, (current, point) => current + point) / points.Count;
        centerPoint.y = points.Min(p => p.y);

        bool generateWall = true;

        if (way.HasTagKey("building"))
        {
            string buildingType = way.GetTagValue("building");
            if (buildingType == "roof") generateWall = false;
        }

        float baseHeight = 15;
        float roofHeight = 0;

        Material wallMaterial;
        Material roofMaterial;

        bool useDefaultMaterials = true;

        if (prefs.buildingMaterials == null || prefs.buildingMaterials.Count == 0)
        {
            wallMaterial = GetMaterialByTags("OSM-House-Wall-Material", way.tags, defHouseWallMaterial);
            roofMaterial = GetMaterialByTags("OSM-House-Roof-Material", way.tags, defHouseRoofMaterial);
        }
        else
        {
            useDefaultMaterials = false;
            int rnd = Random.Range(0, prefs.buildingMaterials.Count);
            wallMaterial = prefs.buildingMaterials[rnd].wall ?? defHouseWallMaterial;
            roofMaterial = prefs.buildingMaterials[rnd].roof ?? defHouseRoofMaterial;
        }

        wallMaterial = (wallMaterial != null) ? Instantiate(wallMaterial) as Material: new Material(Shader.Find("Diffuse"));
        roofMaterial = (roofMaterial != null) ? Instantiate(roofMaterial) as Material : new Material(Shader.Find("Diffuse"));

        RealWorldTerrainOSMRoofType roofType = RealWorldTerrainOSMRoofType.flat;
        AnalizeHouseTags(way, ref wallMaterial, ref roofMaterial, ref baseHeight, useDefaultMaterials);
        AnalizeHouseRoofType(way, ref baseHeight, ref roofType, ref roofHeight);

        Vector3[] baseVerticles = points.Select(p => p - centerPoint).ToArray();

        GameObject houseGO = RealWorldTerrainUtils.CreateGameObject(houseContainer, "House " + way.id);
        houseGO.transform.position = centerPoint + houseContainer.transform.position;

        RealWorldTerrainOSMBuilding house = houseGO.AddComponent<RealWorldTerrainOSMBuilding>();
        house.baseHeight = baseHeight;
        house.baseVerticles = baseVerticles;
        house.container = RealWorldTerrain.container;
        house.roofHeight = roofHeight;
        house.roofType = roofType;

        house.wall = generateWall ? CreateHouseWall(houseGO, baseVerticles, globalContainer.scale, baseHeight, wallMaterial, way.id) : null;
        house.roof = CreateHouseRoof(houseGO, baseVerticles, globalContainer.scale, baseHeight, roofHeight, roofType, roofMaterial, way.id);

        houseGO.AddComponent<RealWorldTerrainMeta>().GetFromOSM(way);
    }

    private static MeshFilter CreateHouseRoof(GameObject houseGO, Vector3[] baseVerticles, Vector3 scale,
        float baseHeight, float roofHeight, RealWorldTerrainOSMRoofType roofType, Material material, string id)
    {
        GameObject wallGO = new GameObject("Roof");
        wallGO.transform.parent = houseGO.transform;
        wallGO.transform.localPosition = Vector3.zero;

        return RealWorldTerrainOSM.AppendMesh(wallGO,
            CreateHouseRoofMesh(baseVerticles, scale, baseHeight, roofHeight, roofType, houseGO.name), material,
            string.Format("House_{0}_Roof", id));
    }

    private static void CreateHouseRoofDome(Vector3 scale, float height, List<Vector3> vertices, List<int> triangles)
    {
        Vector3 roofTopPoint = Vector3.zero;
        roofTopPoint = vertices.Aggregate(roofTopPoint, (current, point) => current + point) / vertices.Count;
        roofTopPoint.y = height * scale.y;
        int vIndex = vertices.Count;

        for (int i = 0; i < vertices.Count; i++)
        {
            int p1 = i;
            int p2 = i + 1;
            if (p2 >= vertices.Count) p2 -= vertices.Count;

            triangles.AddRange(new[] {p1, p2, vIndex});
        }

        vertices.Add(roofTopPoint);
    }

    private static Mesh CreateHouseRoofMesh(Vector3[] baseVerticles, Vector3 scale, float baseHeight, float roofHeight,
        RealWorldTerrainOSMRoofType roofType, string name, bool inverted = false)
    {
        List<Vector2> roofPoints = new List<Vector2>();
        List<Vector3> vertices = new List<Vector3>();

        CreateHouseRoofVerticles(baseVerticles, vertices, roofPoints, scale, baseHeight);
        int[] triangles =
            CreateHouseRoofTriangles(scale, vertices, roofType, roofPoints, baseHeight, roofHeight).ToArray();

        Vector3 side1 = vertices[triangles[1]] - vertices[triangles[0]];
        Vector3 side2 = vertices[triangles[2]] - vertices[triangles[0]];
        Vector3 perp = Vector3.Cross(side1, side2);

        bool reversed = perp.y < 0;
        if (inverted) reversed = !reversed;
        if (reversed) triangles = triangles.Reverse().ToArray();

        float minX = vertices.Min(p => p.x);
        float minZ = vertices.Min(p => p.z);
        float maxX = vertices.Max(p => p.x);
        float maxZ = vertices.Max(p => p.z);
        float offX = maxX - minX;
        float offZ = maxZ - minZ;

        Vector2[] uvs = vertices.Select(v => new Vector2((v.x - minX) / offX, (v.z - minZ) / offZ)).ToArray();

        Mesh mesh = new Mesh
        {
            name = name + " Roof",
            vertices = vertices.ToArray(),
            uv = uvs,
            triangles = triangles.ToArray()
        };

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    private static List<int> CreateHouseRoofTriangles(Vector3 scale, List<Vector3> vertices,
        RealWorldTerrainOSMRoofType roofType, List<Vector2> roofPoints, float baseHeight, float roofHeight)
    {
        List<int> triangles = new List<int>();
        if (roofType == RealWorldTerrainOSMRoofType.flat)
            triangles.AddRange(RealWorldTerrainUtils.Triangulate(roofPoints).Select(index => index));
        else if (roofType == RealWorldTerrainOSMRoofType.dome)
            CreateHouseRoofDome(scale, baseHeight + roofHeight, vertices, triangles);
        return triangles;
    }

    private static void CreateHouseRoofVerticles(Vector3[] baseVerticles, List<Vector3> verticles,
        List<Vector2> roofPoints, Vector3 scale, float baseHeight)
    {
        float topPoint = baseVerticles.Max(v => v.y) + baseHeight * scale.y;
        foreach (Vector3 p in baseVerticles)
        {
            Vector3 tv = new Vector3(p.x, topPoint, p.z);
            Vector2 rp = new Vector2(p.x, p.z);

            if (!verticles.Contains(tv)) verticles.Add(tv);
            if (!roofPoints.Contains(rp)) roofPoints.Add(rp);
        }
    }

    private static MeshFilter CreateHouseWall(GameObject houseGO, Vector3[] baseVerticles, Vector3 scale,
        float baseHeight, Material material, string id)
    {
        GameObject wallGO = new GameObject("Wall");
        wallGO.transform.parent = houseGO.transform;
        wallGO.transform.localPosition = Vector3.zero;

        return RealWorldTerrainOSM.AppendMesh(wallGO,
            CreateHouseWallMesh(baseVerticles, scale, baseHeight, houseGO.name), material, string.Format("House_{0}_Wall", id));
    }

    private static Mesh CreateHouseWallMesh(Vector3[] baseVerticles, Vector3 scale, float baseHeight, string name,
        bool inverted = false)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();

        bool reversed = CreateHouseWallVerticles(scale, baseHeight, baseVerticles, vertices, uvs);
        if (inverted) reversed = !reversed;
        int[] triangles = CreateHouseWallTriangles(vertices, reversed).ToArray();

        Mesh mesh = new Mesh
        {
            name = name + " Wall",
            vertices = vertices.ToArray(),
            uv = uvs.ToArray(),
            triangles = triangles.ToArray()
        };

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    private static List<int> CreateHouseWallTriangles(List<Vector3> vertices, bool reversed)
    {
        List<int> triangles = new List<int>();
        for (int i = 0; i < vertices.Count / 2 - 1; i++)
            triangles.AddRange(GetHouseWallTriangle(vertices.Count, reversed, i));
        return triangles;
    }

    private static bool CreateHouseWallVerticles(Vector3 scale, float baseHeight, Vector3[] baseVerticles,
        List<Vector3> vertices, List<Vector2> uvs)
    {
        float topPoint = baseVerticles.Max(v => v.y) + baseHeight * scale.y;

        foreach (Vector3 p in baseVerticles)
            AddHouseWallVerticle(vertices, p, topPoint);
        AddHouseWallVerticle(vertices, baseVerticles.First(), topPoint);

        float totalDistance = 0;

        for (int i = 0; i < vertices.Count / 2; i++)
        {
            int i1 = Mathf.RoundToInt(Mathf.Repeat(i * 2, vertices.Count));
            int i2 = Mathf.RoundToInt(Mathf.Repeat((i + 1) * 2, vertices.Count));
            totalDistance += (vertices[i1] - vertices[i2]).magnitude;
        }

        float currentDistance = 0;

        for (int i = 0; i < vertices.Count / 2; i++)
        {
            int i1 = Mathf.RoundToInt(Mathf.Repeat(i * 2, vertices.Count));
            int i2 = Mathf.RoundToInt(Mathf.Repeat((i + 1) * 2, vertices.Count));
            float curU = currentDistance / totalDistance;
            uvs.Add(new Vector2(curU, 0));
            uvs.Add(new Vector2(curU, 1));

            currentDistance += (vertices[i1] - vertices[i2]).magnitude;
        }

        int southIndex = -1;
        float southZ = float.MaxValue;

        for (int i = 0; i < baseVerticles.Length; i++)
        {
            if (baseVerticles[i].z < southZ)
            {
                southZ = baseVerticles[i].z;
                southIndex = i;
            }
        }

        int prevIndex = southIndex - 1;
        if (prevIndex < 0) prevIndex = baseVerticles.Length - 1;

        int nextIndex = southIndex + 1;
        if (nextIndex >= baseVerticles.Length) nextIndex = 0;

        float angle1 = RealWorldTerrainUtils.Angle2D(baseVerticles[southIndex], baseVerticles[nextIndex]);
        float angle2 = RealWorldTerrainUtils.Angle2D(baseVerticles[southIndex], baseVerticles[prevIndex]);

        return angle1 < angle2;
    }

    public static void Dispose()
    {
        loaded = false;

        ways = null;
        nodes = null;
        relations = null;

        defHouseRoofMaterial = null;
        defHouseWallMaterial = null;

        baseContainer = null;
        houseContainer = null;
        RealWorldTerrainOSMBuildREditor.alreadyCreated = null;
    }

    public static void Download()
    {
        if (!prefs.generateBuildings || File.Exists(compressedFilename)) return;
        if (File.Exists(filename))
        {
            byte[] data = File.ReadAllBytes(filename);
            OnDownloadComplete(ref data);
        }
        else
        {
            RealWorldTerrainDownloaderItem item = RealWorldTerrainDownloader.Add(url, filename, RealWorldTerrainDownloadType.data, "OSM roads", 600000);
            item.OnComplete += OnDownloadComplete;
        }
    }

    public static string FixPathString(string path)
    {
        return path.Replace(new[] {":", "/", "\\", "="}, "-");
    }

    public static void Generate(RealWorldTerrainContainer globalContainer)
    {
        if (!loaded)
        {
            Load();

            if (ways.Count == 0)
            {
                RealWorldTerrain.phaseComplete = true;
                return;
            }

            if (RealWorldTerrain.generateTarget is RealWorldTerrainItem)
            {
                RealWorldTerrainItem item = RealWorldTerrain.generateTarget as RealWorldTerrainItem;
                baseContainer = RealWorldTerrainUtils.CreateGameObject(globalContainer, "Buildings " + item.x + "x" + (item.container.terrainCount.y - item.y - 1));
                baseContainer.transform.position = item.transform.position;
            }
            else baseContainer = RealWorldTerrainUtils.CreateGameObject(globalContainer, "Buildings");
            houseContainer = RealWorldTerrainUtils.CreateGameObject(baseContainer, "Houses");

            defHouseWallMaterial = RealWorldTerrainEditorUtils.FindMaterial("Default-House-Wall-Material.mat");
            defHouseRoofMaterial = RealWorldTerrainEditorUtils.FindMaterial("Default-House-Roof-Material.mat");
            globalContainer.generatedBuildings = true;
        }

        GenerateHouses(globalContainer);

        if (!RealWorldTerrain.phaseComplete) RealWorldTerrain.phaseProgress = RealWorldTerrain.phaseIndex / (float)(ways.Count);
        else RealWorldTerrain.phaseProgress = 1;
    }

    private static void GenerateHouses(RealWorldTerrainContainer globalContainer)
    {
        long startTime = DateTime.Now.Ticks;

        while (RealWorldTerrain.phaseIndex < ways.Count)
        {
            if (new TimeSpan(DateTime.Now.Ticks - startTime).TotalSeconds > 1) return;

            RealWorldTerrainOSMWay way = ways[RealWorldTerrain.phaseIndex];
            RealWorldTerrain.phaseIndex++;

            if (way.GetTagValue("building") == "bridge") continue;
            string layer = way.GetTagValue("layer");
            if (!String.IsNullOrEmpty(layer) && Int32.Parse(layer) < 0) continue;

            CreateHouse(way, globalContainer);
        }

        RealWorldTerrain.phaseComplete = true;
    }

    private static void GetHeightFromString(string str, ref float height)
    {
        if (!String.IsNullOrEmpty(str))
        {
            if (!float.TryParse(str, out height))
            {
                if (str.Substring(str.Length - 2, 2) == "cm")
                {
                    float.TryParse(str.Substring(0, str.Length - 2), out height);
                    height /= 10;
                }
                else if (str.Substring(str.Length - 1, 1) == "m")
                    float.TryParse(str.Substring(0, str.Length - 1), out height);
            }
        }
    }

    private static int[] GetHouseWallTriangle(int countVertices, bool reversed, int i)
    {
        int p1 = i * 2;
        int p2 = (i + 1) * 2;
        int p3 = (i + 1) * 2 + 1;
        int p4 = i * 2 + 1;

        if (p2 >= countVertices) p2 -= countVertices;
        if (p3 >= countVertices) p3 -= countVertices;

        if (reversed) return new[] {p1, p4, p3, p1, p3, p2};
        return new[] {p2, p3, p1, p3, p4, p1};
    }

    public static Material GetMaterialByTags(string materialName, List<RealWorldTerrainOSMTag> tags, Material defaultMaterial)
    {
        if (RealWorldTerrainOSM.projectMaterials == null)
            RealWorldTerrainOSM.projectMaterials = Directory.GetFiles("Assets", "*.mat", SearchOption.AllDirectories);
        foreach (RealWorldTerrainOSMTag tag in tags)
        {
            string matName = string.Format("{0}({1}={2})", materialName, FixPathString(tag.key), FixPathString(tag.value));
            foreach (string projectMaterial in RealWorldTerrainOSM.projectMaterials)
            {
                if (projectMaterial.Contains(matName))
                {
                    string assetFN = projectMaterial.Replace("\\", "/");
                    return new Material((Material)AssetDatabase.LoadAssetAtPath(assetFN, typeof(Material)));
                }
            }
        }

        return new Material(defaultMaterial) { color = Color.white }; ;
    }

    public static void Load()
    {
        RealWorldTerrainOSM.LoadOSM(compressedFilename, out nodes, out ways, out relations);
        loaded = true;
    }

    private static void OnDownloadComplete(ref byte[] data)
    {
        RealWorldTerrainOSM.GenerateCompressedFile(data, ref nodes, ref ways, ref relations, compressedFilename);
    }

    public void OnEnable()
    {
        building = (RealWorldTerrainOSMBuilding) target;
    }

    public override void OnInspectorGUI()
    {
        building.baseHeight = EditorGUILayout.FloatField("Base Height (meters): ", building.baseHeight);

        building.roofType =
            (RealWorldTerrainOSMRoofType) EditorGUILayout.EnumPopup("Roof type: ", building.roofType);
        if (building.roofType != RealWorldTerrainOSMRoofType.flat)
            building.roofHeight = EditorGUILayout.FloatField("Roof Height (meters): ", building.roofHeight);

        if (GUILayout.Button("Invert wall normals") && building.wall != null)
        {
            building.invertWall = !building.invertWall;
            building.wall.mesh =
                building.wall.sharedMesh =
                    CreateHouseWallMesh(building.baseVerticles, building.container.scale, building.baseHeight,
                        building.name,
                        building.invertWall);
        }

        if (GUILayout.Button("Invert roof normals") && building.roof != null)
        {
            building.invertRoof = !building.invertRoof;
            building.roof.mesh =
                building.roof.sharedMesh =
                    CreateHouseRoofMesh(building.baseVerticles, building.container.scale, building.baseHeight,
                        building.roofHeight, building.roofType, building.name, building.invertRoof);
        }

        if (GUILayout.Button("Update"))
        {
            if (building.wall != null)
            {
                building.wall.mesh =
                    building.wall.sharedMesh =
                        CreateHouseWallMesh(building.baseVerticles, building.container.scale, building.baseHeight,
                            building.name,
                            building.invertWall);
            }

            if (building.roof != null)
            {
                building.roof.mesh =
                    building.roof.sharedMesh =
                        CreateHouseRoofMesh(building.baseVerticles, building.container.scale, building.baseHeight,
                            building.roofHeight, building.roofType, building.name, building.invertRoof);
            }
        }

        if (GUILayout.Button("Export mesh to OBJ"))
        {
            string path = EditorUtility.SaveFilePanel("Save building to OBJ", "", building.name + ".obj", "obj");
            if (path.Length != 0)
            {
                RealWorldTerrainUtils.ExportMesh(path, building.wall, building.roof);
            }
        }
    }
}