/*     INFINITY CODE 2013-2015      */
/*   http://www.infinity-code.com   */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEngine;

public class RealWorldTerrainOSMGrass
{
    private static List<int[,]> details;
    private static List<RealWorldTerrainOSMWay> grassWays;
    private static int totalCount;
    private static List<string> alreadyCreated;
    private static float[] detailsInPoint;

    public static List<RealWorldTerrainOSMNode> nodes;
    public static List<RealWorldTerrainOSMWay> ways;
    public static List<RealWorldTerrainOSMRelation> relations;
    public static bool loaded;

    private static RealWorldTerrainPrefs prefs
    {
        get { return RealWorldTerrain.prefs; }
    }

    public static string url
    {
        get
        {
            string format = string.Format("node({0},{1},{2},{3});way(bn)['landuse'~'grass|forest|meadow|park|pasture|recreation_ground'];(._;>;);out;node({0},{1},{2},{3});way(bn)['natural'~'scrub|wood']; (._;>;);out;node({0},{1},{2},{3});way(bn)['leisure'~'park|golf_course'];(._;>;);out;node({0},{1},{2},{3});rel(bn)['leisure'~'golf_course']; (._;>;);out;",
                prefs.coordinatesTo.y, prefs.coordinatesFrom.x, prefs.coordinatesFrom.y, prefs.coordinatesTo.x);
            return RealWorldTerrainUtils.overpassAPI + format;
        }
    }

    public static string filename
    {
        get
        {
            return Path.Combine(RealWorldTerrainEditorUtils.osmCacheFolder, string.Format("grass_{0}_{1}_{2}_{3}.osm", prefs.coordinatesTo.y, prefs.coordinatesFrom.x, prefs.coordinatesFrom.y, prefs.coordinatesTo.x));
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
        loaded = false;

        nodes = null;
        ways = null;
        relations = null;

        details = null;
        grassWays = null;
        alreadyCreated = null;
    }

    public static void Download()
    {
        if (!prefs.generateGrass || File.Exists(compressedFilename)) return;
        if (File.Exists(filename))
        {
            byte[] data = File.ReadAllBytes(filename);
            OnDownloadComplete(ref data);
        }
        else
        {
            RealWorldTerrainDownloaderItem item = RealWorldTerrainDownloader.Add(url, filename, RealWorldTerrainDownloadType.data, "OSM grass", 600000);
            item.OnComplete += OnDownloadComplete;
        }
    }

    public static void Generate(RealWorldTerrainContainer container)
    {
        TerrainData tdata = container.terrains[0, 0].terrain.terrainData;
        int detailResolution = tdata.detailResolution;

        if (!loaded)
        {
            RealWorldTerrainOSM.LoadOSM(compressedFilename, out nodes, out ways, out relations);
            loaded = true;

            container.generatedGrass = true;
            alreadyCreated = new List<string>();

            List<DetailPrototype> prototypes = new List<DetailPrototype>();

            foreach (Texture2D grassPrefab in prefs.grassPrefabs)
            {
                DetailPrototype prototype = new DetailPrototype
                {
                    prototypeTexture = grassPrefab,
                    renderMode = DetailRenderMode.GrassBillboard
                };
                prototypes.Add(prototype);
            }

            details = new List<int[,]>(container.terrains.Length);
            foreach (RealWorldTerrainItem item in container.terrains)
            {
                item.terrain.terrainData.detailPrototypes = prototypes.ToArray();
                for (int i = 0; i < prefs.grassPrefabs.Count; i++) details.Add(new int[detailResolution, detailResolution]);
            }

            detailsInPoint = new float[prefs.grassPrefabs.Count];

            grassWays = ways.FindAll(
                w =>
                    w.HasTags("landuse", "grass", "forest", "meadow", "park", "pasture", "recreation_ground") ||
                    w.HasTags("leisure", "park", "golf_course") || w.HasTags("natural", "scrub", "wood"));
            List<RealWorldTerrainOSMRelation> grassRelations = relations.FindAll(r => r.HasTag("leisure", "golf_course"));
            grassWays.AddRange(grassRelations.SelectMany(r => r.members.Select(m => ways.FirstOrDefault(w => m.reference == w.id))));
            totalCount = grassWays.Count + container.terrainCount.x;

            if (grassWays.Count == 0)
            {
                RealWorldTerrain.phaseComplete = true;
                return;
            }
        }

        float rScaleX = detailResolution / (tdata.heightmapResolution * tdata.heightmapScale.x);
        float rScaleY = detailResolution / (tdata.heightmapResolution * tdata.heightmapScale.z);

        long startTime = DateTime.Now.Ticks;
        float density = prefs.grassDensity / 100f;
        if (density > 1) density = 1;

        if (RealWorldTerrain.phaseIndex < grassWays.Count)
        {
            for (int i = RealWorldTerrain.phaseIndex; i < grassWays.Count; i++)
            {
                RealWorldTerrainOSMWay way = grassWays[i];

                if (alreadyCreated.Contains(way.id)) continue;
                alreadyCreated.Add(way.id);

                List<Vector3> points = RealWorldTerrainOSM.GetGlobalPointsFromWay(way, nodes);

                for (int j = 0; j < points.Count; j++)
                {
                    Vector3 p = RealWorldTerrainEditorUtils.GlobalToLocalWithElevation(points[j], container.scale);
                    p.y = RealWorldTerrainEditorUtils.NormalizeElevation(container, p.y);
                    points[j] = p;
                }

                Rect r = RealWorldTerrainUtils.GetRectFromPoints(points);
                r = new Rect(Mathf.Floor(r.x * rScaleX), Mathf.Floor(r.y * rScaleY), Mathf.Floor(r.width * rScaleX + 0.5f),
                    Mathf.Floor(r.height * rScaleY + 0.5f));

                for (int x = (int)r.xMin; x < r.xMax; x++)
                {
                    for (int y = (int)r.yMin; y < r.yMax; y++)
                    {
                        int tix = Mathf.FloorToInt(x / (float)detailResolution);
                        int tiy = Mathf.FloorToInt(y / (float)detailResolution);
                        if (tix >= prefs.terrainCount.x || tiy >= prefs.terrainCount.y ||
                            tix < 0 || tiy < 0) continue;

                        int tIndex = tix * prefs.terrainCount.y + tiy;
                        if (tIndex < 0 || tIndex >= container.terrains.Length) continue;
                        int tx = x - tIndex * detailResolution;
                        int ty = y - tIndex * detailResolution;
                        bool intersect = RealWorldTerrainUtils.IsPointInPolygon(points, x / rScaleX, y / rScaleY);
                        try
                        {
                            if (intersect)
                            {
                                int tIndex2 = tIndex * prefs.grassPrefabs.Count;
                                if (prefs.grassPrefabs.Count == 1) detailsInPoint[0] = 1;
                                else for (int k = 0; k < prefs.grassPrefabs.Count; k++) detailsInPoint[k] = UnityEngine.Random.Range(0f, 1f);
                                float totalInPoint = detailsInPoint.Sum();
                                if (totalInPoint == 0) continue;

                                for (int k = 0; k < prefs.grassPrefabs.Count; k++)
                                {
                                    details[tIndex2 + k][ty, tx] = Mathf.FloorToInt(detailsInPoint[k] / totalInPoint * 128 * density);
                                    if (details[tIndex2 + k][ty, tx] > 255) details[tIndex2 + k][ty, tx] = 255;
                                }
                            }
                        }
                        catch
                        { }
                    }
                }

                if (new TimeSpan(DateTime.Now.Ticks - startTime).TotalSeconds > 1)
                {
                    RealWorldTerrain.phaseIndex = i + 1;
                    RealWorldTerrain.phaseProgress = RealWorldTerrain.phaseIndex / (float)totalCount;
                    return;
                }
            }
            RealWorldTerrain.phaseIndex = grassWays.Count;
        }

        if (RealWorldTerrain.phaseIndex >= grassWays.Count)
        {
            for (int i = RealWorldTerrain.phaseIndex - grassWays.Count; i < container.terrainCount.x; i++)
            {
                for (int j = 0; j < container.terrainCount.y; j++)
                {
                    int tIndex = i * prefs.terrainCount.y + j;
                    for (int k = 0; k < prefs.grassPrefabs.Count; k++)
                    {
                        container.terrains[i, j].terrain.terrainData.SetDetailLayer(0, 0, k,
                            details[tIndex * prefs.grassPrefabs.Count + k]);
                    }
                }
                if (new TimeSpan(DateTime.Now.Ticks - startTime).TotalSeconds > 1)
                {
                    RealWorldTerrain.phaseIndex = grassWays.Count + i + 1;
                    RealWorldTerrain.phaseProgress = RealWorldTerrain.phaseIndex / (float)totalCount;
                    return;
                }
            }
        }

        Dispose();
        GC.Collect();
        RealWorldTerrain.phaseComplete = true;
    }

    private static void OnDownloadComplete(ref byte[] data)
    {
        RealWorldTerrainOSM.GenerateCompressedFile(data, ref nodes, ref ways, ref relations, compressedFilename);
    }
}