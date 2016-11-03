/*     INFINITY CODE 2013-2015      */
/*   http://www.infinity-code.com   */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEngine;

public class RealWorldTerrainOSMTree
{
    private static List<RealWorldTerrainOSMNode> nodes;
    private static List<RealWorldTerrainOSMWay> ways;
    private static List<RealWorldTerrainOSMRelation> relations;
    private static bool loaded;

    private static List<RealWorldTerrainOSMNode> treeNodes;
    private static List<RealWorldTerrainOSMWay> woodWays;
    private static List<RealWorldTerrainOSMWay> treeRowWays;
    private static float treeDensity;
    private static int totalTreeCount;
    private static List<string> alreadyCreated;

    private static RealWorldTerrainPrefs prefs
    {
        get { return RealWorldTerrain.prefs; }
    }

    public static string url
    {
        get
        {
            string format = string.Format("node({0},{1},{2},{3})['natural'~'tree'];(._;>;);out;node({0},{1},{2},{3});(way(bn)['natural'~'wood|tree_row'];way(bn)['landuse'~'forest|park'];);(._;>;);out;", 
                prefs.coordinatesTo.y, prefs.coordinatesFrom.x, prefs.coordinatesFrom.y, prefs.coordinatesTo.x);
            return RealWorldTerrainUtils.overpassAPI + format;
        }
    }

    public static string filename
    {
        get
        {
            return Path.Combine(RealWorldTerrainEditorUtils.osmCacheFolder, string.Format("trees_{0}_{1}_{2}_{3}.osm", prefs.coordinatesTo.y, prefs.coordinatesFrom.x, prefs.coordinatesFrom.y, prefs.coordinatesTo.x));
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

        relations = null;
        ways = null;
        nodes = null;

        treeNodes = null;
        woodWays = null;
        treeRowWays = null;
        alreadyCreated = null;
    }

    public static void Download()
    {
        if (!prefs.generateTrees || File.Exists(compressedFilename)) return;
        if (File.Exists(filename))
        {
            byte[] data = File.ReadAllBytes(filename);
            OnDownloadComplete(ref data);
        }
        else
        {
            RealWorldTerrainDownloaderItem item = RealWorldTerrainDownloader.Add(url, filename, RealWorldTerrainDownloadType.data, "OSM trees", 600000);
            item.OnComplete += OnDownloadComplete;
        }
    }

    public static void Generate(RealWorldTerrainContainer container)
    {
        if (prefs.treePrefabs.Count == 0)
        {
            RealWorldTerrain.phaseComplete = true;
            return;
        }

        if (!loaded)
        {
            RealWorldTerrainOSM.LoadOSM(compressedFilename, out nodes, out ways, out relations);
            loaded = true;

            container.generatedTrees = true;

            TreePrototype[] prototypes =
                prefs.treePrefabs.Select(prefab => new TreePrototype { prefab = prefab }).ToArray();
            foreach (RealWorldTerrainItem item in container.terrains) item.terrain.terrainData.treePrototypes = prototypes;

            treeNodes = nodes.FindAll(n => n.HasTag("natural", "tree"));
            woodWays = ways.FindAll(w => w.HasTag("natural", "wood") || w.HasTags("landuse", "forest", "park"));
            treeRowWays = ways.FindAll(w => w.HasTag("natural", "tree_row"));

            if (treeNodes.Count == 0 && woodWays.Count == 0 && treeRowWays.Count == 0)
            {
                RealWorldTerrain.phaseComplete = true;
                return;
            }

            alreadyCreated = new List<string>();

            treeDensity = 800f / prefs.treeDensity;
            totalTreeCount = treeNodes.Count + treeRowWays.Count + woodWays.Count;
        }

        long startTime = DateTime.Now.Ticks;

        if (RealWorldTerrain.phaseIndex < treeNodes.Count)
        {
            for (int i = RealWorldTerrain.phaseIndex; i < treeNodes.Count; i++)
            {
                RealWorldTerrainOSMNode node = treeNodes[i];
                if (alreadyCreated.Contains(node.id)) continue;
                alreadyCreated.Add(node.id);

                Vector3 pos =
                    Vector3.Scale(
                        new Vector3(node.lon - prefs.coordinatesFrom.x, 0,
                            node.lat - prefs.coordinatesTo.y), container.scale);

                SetTreeToTerrain(container, pos);

                if (new TimeSpan(DateTime.Now.Ticks - startTime).TotalSeconds > 1)
                {
                    RealWorldTerrain.phaseIndex = i + 1;
                    RealWorldTerrain.phaseProgress = RealWorldTerrain.phaseIndex / (float)totalTreeCount;
                    return;
                }
            }
            RealWorldTerrain.phaseIndex = treeNodes.Count;
        }

        if (RealWorldTerrain.phaseIndex < treeRowWays.Count + treeNodes.Count)
        {
            for (int index = RealWorldTerrain.phaseIndex - treeNodes.Count; index < treeRowWays.Count; index++)
            {
                RealWorldTerrainOSMWay way = treeRowWays[index];
                if (alreadyCreated.Contains(way.id)) continue;
                alreadyCreated.Add(way.id);
                List<Vector3> points = RealWorldTerrainOSM.GetGlobalPointsFromWay(way, nodes);

                for (int i = 0; i < points.Count; i++)
                {
                    Vector3 p = RealWorldTerrainEditorUtils.GlobalToLocalWithElevation(points[i], container.scale);
                    p.y = RealWorldTerrainEditorUtils.NormalizeElevation(container, p.y);
                    points[i] = p;
                }

                for (int i = 0; i < points.Count - 1; i++)
                {
                    int len = Mathf.RoundToInt((points[i] - points[i + 1]).magnitude / treeDensity);
                    if (len > 0)
                    {
                        for (int j = 0; j <= len; j++)
                            SetTreeToTerrain(container, Vector3.Lerp(points[i], points[i + 1], j / (float)len));
                    }
                    else SetTreeToTerrain(container, points[i]);
                }

                if (new TimeSpan(DateTime.Now.Ticks - startTime).TotalSeconds > 1)
                {
                    RealWorldTerrain.phaseIndex = index + treeNodes.Count + 1;
                    RealWorldTerrain.phaseProgress = RealWorldTerrain.phaseIndex / (float)totalTreeCount;
                    return;
                }
            }
            RealWorldTerrain.phaseIndex = treeNodes.Count + treeRowWays.Count;
        }

        if (RealWorldTerrain.phaseIndex < treeRowWays.Count + treeNodes.Count + woodWays.Count)
        {
            for (int i = RealWorldTerrain.phaseIndex - treeRowWays.Count - treeNodes.Count; i < woodWays.Count; i++)
            {
                RealWorldTerrainOSMWay way = woodWays[i];
                if (alreadyCreated.Contains(way.id)) continue;
                alreadyCreated.Add(way.id);
                List<Vector3> points = RealWorldTerrainOSM.GetGlobalPointsFromWay(way, nodes);

                for (int j = 0; j < points.Count; j++)
                {
                    Vector3 p = RealWorldTerrainEditorUtils.GlobalToLocalWithElevation(points[j], container.scale);
                    p.y = RealWorldTerrainEditorUtils.NormalizeElevation(container, p.y);
                    points[j] = p;
                }

                Rect rect = RealWorldTerrainUtils.GetRectFromPoints(points);
                int lx = Mathf.RoundToInt(rect.width / treeDensity);
                int ly = Mathf.RoundToInt(rect.height / treeDensity);

                if (lx > 0 && ly > 0) GenerateWoodsInArea(container, lx, ly, rect, points);

                if (new TimeSpan(DateTime.Now.Ticks - startTime).TotalSeconds > 1)
                {
                    RealWorldTerrain.phaseIndex = i + treeNodes.Count + treeRowWays.Count + 1;
                    RealWorldTerrain.phaseProgress = RealWorldTerrain.phaseIndex / (float)totalTreeCount;
                    return;
                }
            }

            RealWorldTerrain.phaseIndex = totalTreeCount;
        }

        Dispose();
        GC.Collect();
        RealWorldTerrain.phaseComplete = true;
    }

    private static void GenerateWoodsInArea(RealWorldTerrainContainer container, int lx, int ly, Rect rect,
        List<Vector3> points)
    {
        float rVal = 800f / prefs.treeDensity;
        for (int x = 0; x < lx; x++)
        {
            for (int y = 0; y < ly; y++)
            {
                Vector3 p = new Vector3(Mathf.Lerp(rect.xMin, rect.xMax, x / (float)lx), 0,
                    Mathf.Lerp(rect.yMin, rect.yMax, y / (float)ly));
                p += new Vector3(UnityEngine.Random.Range(-rVal, rVal), 0, UnityEngine.Random.Range(-rVal, rVal));

                if (RealWorldTerrainUtils.IsPointInPolygon(points, p)) SetTreeToTerrain(container, p);
            }
        }
    }

    private static void OnDownloadComplete(ref byte[] data)
    {
        RealWorldTerrainOSM.GenerateCompressedFile(data, ref nodes, ref ways, ref relations, compressedFilename);
    }

    private static void SetTreeToTerrain(RealWorldTerrainContainer container, Vector3 pos)
    {
        foreach (RealWorldTerrainItem item in container.terrains)
        {
            TerrainData tData = item.terrain.terrainData;
            Vector3 terPos = item.terrain.transform.position;
            Vector3 localPos = pos - terPos;
            int heightmapWidth = tData.heightmapWidth - 1;
            int heightmapHeight = tData.heightmapHeight - 1;
            if (localPos.x > 0 && localPos.z > 0 && localPos.x < heightmapWidth * tData.heightmapScale.x &&
                localPos.z < heightmapHeight * tData.heightmapScale.z)
            {
                item.terrain.AddTreeInstance(new TreeInstance
                {
                    color = Color.white,
                    heightScale = 1 + UnityEngine.Random.Range(-0.3f, 0.3f),
                    lightmapColor = Color.white,
                    position =
                        new Vector3(localPos.x / (heightmapWidth * tData.heightmapScale.x), 0,
                            localPos.z / (heightmapHeight * tData.heightmapScale.z)),
                    prototypeIndex = UnityEngine.Random.Range(0, tData.treePrototypes.Length),
                    widthScale = 1 + UnityEngine.Random.Range(-0.2f, 0.2f)
                });
                break;
            }
        }
    }
}