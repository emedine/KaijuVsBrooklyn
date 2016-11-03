using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static class RealWorldTerrainOSMRivers
{
    private static List<RealWorldTerrainOSMNode> nodes;
    private static List<RealWorldTerrainOSMWay> ways;
    private static List<RealWorldTerrainOSMRelation> relations;
    private static bool loaded;

    public static string url
    {
        get
        {
            string format = string.Format("node({0},{1},{2},{3});way(bn);rel(bw)['waterway'];(._;>;);out;", prefs.coordinatesTo.y, prefs.coordinatesFrom.x, prefs.coordinatesFrom.y, prefs.coordinatesTo.x);
            return RealWorldTerrainUtils.overpassAPI + format;
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
            return Path.Combine(RealWorldTerrainEditorUtils.osmCacheFolder, string.Format("rivers_{0}_{1}_{2}_{3}.osm", prefs.coordinatesTo.y, prefs.coordinatesFrom.x, prefs.coordinatesFrom.y, prefs.coordinatesTo.x));
        }
    }

    public static string compressedFilename
    {
        get
        {
            return filename + "c";
        }
    }

    public static void Dispose()
    {
        nodes = null;
        ways = null;
        relations = null;
        loaded = false;
    }

    private static void CreateRiver(RealWorldTerrainOSMRelation relation, GameObject container, RealWorldTerrainContainer globalContainer, Material defMaterial)
    {
        Vector3 position;
        List<Vector2> uv = new List<Vector2>();
        List<Vector4> tangents = new List<Vector4>();
        List<Vector3> verticles = GetRiverVerticles(relation, globalContainer, uv, tangents, out position);
        List<Vector2> flatPoints = verticles.Select(v => new Vector2(v.x, v.z)).ToList();

        int[] triangles = RealWorldTerrainUtils.Triangulate(flatPoints).ToArray();
        bool reversed = RealWorldTerrainUtils.IsClockWise(verticles[triangles[0]], verticles[triangles[1]],
            verticles[triangles[2]]);
        if (reversed) triangles = triangles.Reverse().ToArray();

        GameObject meshGO = RealWorldTerrainUtils.CreateGameObject(container, "River " + relation.id);
        meshGO.transform.localPosition = position;

        Mesh mesh = new Mesh
        {
            name = meshGO.name,
            vertices = verticles.ToArray(),
            uv = uv.ToArray(),
            tangents = tangents.ToArray(),
            triangles = triangles.ToArray()
        };

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        RealWorldTerrainOSM.AppendMesh(meshGO, mesh, new Material(defMaterial), string.Format("River_{0}", relation.id));
    }

    public static void Download()
    {
        if (!prefs.generateRivers || File.Exists(compressedFilename)) return;
        if (File.Exists(filename))
        {
            byte[] data = File.ReadAllBytes(filename);
            OnDownloadComplete(ref data);
        }
        else
        {
            RealWorldTerrainDownloaderItem item = RealWorldTerrainDownloader.Add(url, filename, RealWorldTerrainDownloadType.data, "OSM rivers", 600000);
            item.OnComplete += OnDownloadComplete;
        }
    }

    private static List<RealWorldTerrainOSMWay> GetRiverAviableWays(RealWorldTerrainOSMRelation relation)
    {
        List<RealWorldTerrainOSMWay> aviableWays = new List<RealWorldTerrainOSMWay>();

        foreach (RealWorldTerrainOSMRelationMember member in relation.members)
        {
            int wayIndex = ways.FindIndex(w => w.id == member.reference);
            if (wayIndex == -1) continue;

            aviableWays.Add(ways[wayIndex]);
        }

        return aviableWays;
    }

    private static List<Vector3> GetRiverVerticles(RealWorldTerrainOSMRelation relation, RealWorldTerrainContainer globalContainer, List<Vector2> uv,
        List<Vector4> tangents, out Vector3 position)
    {
        List<RealWorldTerrainOSMWay> aviableWays = GetRiverAviableWays(relation);
        List<Vector3> verticles = GetRiverVerticlesFromAV(globalContainer, aviableWays);

        for (int i = verticles.Count - 1; i > 0; i--)
            if (verticles.LastIndexOf(verticles[i], i - 1) != -1) verticles.RemoveAt(i);

        float minX = verticles.Min(v => v.x);
        float maxX = verticles.Max(v => v.x);

        float minY = verticles.Min(v => v.y);
        float maxY = verticles.Max(v => v.y);

        float minZ = verticles.Min(v => v.z);
        float maxZ = verticles.Max(v => v.z);

        float offX = maxX - minX;
        float offZ = maxZ - minZ;

        position = new Vector3((maxX + minX) / 2, (maxY + minY) / 2, (maxZ + minZ) / 2);

        for (int i = 0; i < verticles.Count; i++)
        {
            Vector3 v = verticles[i];

            v -= position;
            verticles[i] = v;
            uv.Add(new Vector2((v.x - minX) / offX, (v.z - minZ) / offZ));
            tangents.Add(new Vector4(0, 1, 0, 1));
        }

        return verticles;
    }

    private static List<Vector3> GetRiverVerticlesFromAV(RealWorldTerrainContainer globalContainer, List<RealWorldTerrainOSMWay> availableWays)
    {
        bool testDirection = true;
        bool reversed = false;

        List<Vector3> verticles = new List<Vector3>();
        foreach (RealWorldTerrainOSMWay way in availableWays)
        {
            List<Vector3> points = RealWorldTerrainOSM.GetGlobalPointsFromWay(way, nodes);

            for (int i = 0; i < points.Count; i++)
            {
                Vector3 p = RealWorldTerrainEditorUtils.GlobalToLocalWithElevation(points[i], globalContainer.scale);
                p.y = RealWorldTerrainEditorUtils.NormalizeElevation(globalContainer, p.y);
                points[i] = p;
            }

            if (verticles.Count > 0 && testDirection)
            {
                Vector3 lastVerticle = verticles.Last();
                Vector3 firstPoint = points.First();
                Vector3 lastPoint = points.Last();
                float off1 = (lastVerticle - lastPoint).magnitude;
                float off2 = (lastVerticle - firstPoint).magnitude;

                Vector3 firstVerticle = verticles.First();
                float off3 = (firstVerticle - firstPoint).magnitude;
                float off4 = (firstVerticle - lastPoint).magnitude;

                float min = Mathf.Min(off1, off2, off3, off4);

                reversed = min == off3 || min == off4;
                testDirection = false;
            }
            if (!reversed) verticles.AddRange(points);
            else
            {
                if (verticles.First() == points.First()) points.Reverse();
                verticles.InsertRange(0, points);
            }
        }
        return verticles;
    }

    public static void Generate(RealWorldTerrainContainer baseContainer)
    {
        if (!loaded) Load();

        baseContainer.generatedRivers = true;
        GameObject container = new GameObject("Rivers");
        container.transform.parent = baseContainer.gameObject.transform;
        container.transform.localPosition = Vector3.zero;

        Material defMaterial = RealWorldTerrainEditorUtils.FindMaterial("Default-River-Material.mat");

        foreach (RealWorldTerrainOSMRelation relation in relations) CreateRiver(relation, container, baseContainer, defMaterial);
    }

    public static void Load()
    {
        if (loaded) return;
        RealWorldTerrainOSM.LoadOSM(compressedFilename, out nodes, out ways, out relations);
        loaded = true;
    }

    private static void OnDownloadComplete(ref byte[] data)
    {
        RealWorldTerrainOSM.GenerateCompressedFile(data, ref nodes, ref ways, ref relations, compressedFilename);
    }
}