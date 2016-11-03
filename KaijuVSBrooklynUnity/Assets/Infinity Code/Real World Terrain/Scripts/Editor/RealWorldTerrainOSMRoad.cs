/*     INFINITY CODE 2013-2015      */
/*   http://www.infinity-code.com   */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEngine;
using Object = UnityEngine.Object;

#if ROADARCHITECT
using GSD.Roads;
#endif

public class RealWorldTerrainOSMRoad
{
    private static List<RealWorldTerrainOSMRoad> roads;
    private static GameObject roadContainer;

#if ROADARCHITECT
    private static GSDRoadSystem tRoadSystem;
    private List<GSDSplineN> splines;
#endif

    public bool isDuplicate = false;
    public string type;

    private RealWorldTerrainContainer container;
    private string id;
    private List<Vector3> points;
    private RealWorldTerrainOSMWay way;
    private List<Vector3> globalPoints;
    private GameObject roadGo;
    private static List<string> alreadyCreated;
    private static bool loaded = false;
    private static List<RealWorldTerrainOSMNode> nodes;
    private static List<RealWorldTerrainOSMWay> ways;
    private static List<RealWorldTerrainOSMRelation> relations;

    public static string url
    {
        get
        {
            string highwayType = "'highway'";
            if ((int)prefs.roadTypes != -1)
            {
                BitArray ba = new BitArray(System.BitConverter.GetBytes((int)prefs.roadTypes));
                List<string> types = new List<string>();
                for(int i = 0; i < 32; i++) if (ba.Get(i)) types.Add(((RealWorldTerrainOSMRoadType)i).ToString());
                highwayType += "~'" + string.Join(@"|", types.ToArray()) + "'";
            }
            string data = string.Format("node({0},{1},{2},{3});way(bn)[{4}];(._;>;);out;", prefs.coordinatesTo.y, prefs.coordinatesFrom.x, prefs.coordinatesFrom.y, prefs.coordinatesTo.x, highwayType);
            return RealWorldTerrainUtils.overpassAPI + data;
        }
    }

    public static string filename
    {
        get
        {
            return Path.Combine(RealWorldTerrainEditorUtils.osmCacheFolder, string.Format("roads_{0}_{1}_{2}_{3}_{4}.osm", prefs.coordinatesTo.y, prefs.coordinatesFrom.x,
                prefs.coordinatesFrom.y, prefs.coordinatesTo.x, (int)prefs.roadTypes));
        }
    }

    public static string compressedFilename
    {
        get
        {
            return filename + "c";
        }
    }

    private static RealWorldTerrainPrefs prefs
    {
        get { return RealWorldTerrain.prefs; }
    }

    private Vector3 secondPoint
    {
        get { return points[1]; }
    }

    private Vector3 preLastPoint
    {
        get { return points[points.Count - 2]; }
    }

    private Vector3 lastPoint
    {
        get { return points.Last(); }
    }

    private Vector3 firstPoint
    {
        get { return points.First(); }
    }

    public RealWorldTerrainOSMRoad(RealWorldTerrainOSMWay way, RealWorldTerrainContainer container)
    {
        if (roads == null) roads = new List<RealWorldTerrainOSMRoad>();

        this.container = container;
        this.way = way;
        id = way.id;
        type = this.way.GetTagValue("highway");

        globalPoints = RealWorldTerrainOSM.GetGlobalPointsFromWay(this.way, nodes);

        DetectDuplicates();
        if (isDuplicate) return;

        points = new List<Vector3>();
        foreach (Vector3 gp in globalPoints) {
            Vector3 p = RealWorldTerrainEditorUtils.GlobalToLocalWithElevation(gp, container.scale);
            p.y = RealWorldTerrainEditorUtils.NormalizeElevation(container, p.y);
            points.Add(p);
        }

        NormalizePoints();
        NormalizeDistance();
        TrimPoints();

        roads.Add(this);
    }

    private void DetectDuplicates()
    {
        for (int i = 0; i < roads.Count; i++)
        {
            RealWorldTerrainOSMRoad r = roads[i];
            if (r.globalPoints.Count != globalPoints.Count) continue;

            bool findDiff = globalPoints.Where((t, j) => (r.globalPoints[j] - t).magnitude > 0.0001f).Any();
            if (!findDiff)
            {
                isDuplicate = true;
                return;
            }
        }
    }

    public static void Dispose()
    {
        loaded = false;
        ways = null;
        relations = null;
        nodes = null;
        roadContainer = null;
        alreadyCreated = null;
    }

    public static void Download()
    {
        if (!prefs.generateRoads || prefs.roadTypes == 0 || File.Exists(compressedFilename)) return;
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

    public static void CombineRoads()
    {
        if (roads.Count < 2) return;
        int index = 1;
        while (index < roads.Count)
        {
            RealWorldTerrainOSMRoad road2 = roads[index];
            if (road2.points.Count < 2)
            {
                index++;
                continue;
            }

            bool merged = false;
            for (int i = 0; i < index; i++)
            {
                RealWorldTerrainOSMRoad road1 = roads[i];

                if (road1.type != road2.type) continue;
                if (road1.points.Count < 2) continue;

                const float offset = 1;
                if ((road2.firstPoint - road1.lastPoint).magnitude < offset && RealWorldTerrainUtils.Angle2D(road1.preLastPoint, road1.lastPoint, road2.secondPoint) < 20)
                {
                    road1.points.AddRange(road2.points.GetRange(1, road2.points.Count - 1));
                    roads.Remove(road2);
                    merged = true;
                    break;
                }
                if ((road2.lastPoint - road1.firstPoint).magnitude < offset && RealWorldTerrainUtils.Angle2D(road1.secondPoint, road1.firstPoint, road2.preLastPoint) < 20)
                {
                    road1.points.InsertRange(0, road2.points.GetRange(0, road2.points.Count - 1));
                    roads.Remove(road2);
                    merged = true;
                    break;
                }
                if ((road2.lastPoint - road1.lastPoint).magnitude < offset && RealWorldTerrainUtils.Angle2D(road1.preLastPoint, road1.lastPoint, road2.preLastPoint) < 20)
                {
                    List<Vector3> r2points = road2.points.GetRange(0, road2.points.Count - 1);
                    r2points.Reverse();
                    road1.points.AddRange(r2points);
                    roads.Remove(road2);
                    merged = true;
                    break;
                }
                if ((road2.firstPoint - road1.firstPoint).magnitude < offset && RealWorldTerrainUtils.Angle2D(road1.secondPoint, road1.firstPoint, road2.secondPoint) < 20)
                {
                    List<Vector3> r2points = road2.points.GetRange(1, road2.points.Count - 1);
                    r2points.Reverse();
                    road1.points.InsertRange(0, r2points);
                    roads.Remove(road2);
                    merged = true;
                    break;
                }
            }
            if (!merged) index++;
        }
    }

    private void CreateEasyRoadsRoad()
    {
#if EASYROADS
        GameObject roadGO = RealWorldTerrainUtils.CreateGameObject(roadContainer, way.id);
        roadGO.AddComponent<RealWorldTerrainMeta>().GetFromOSM(way);
        RoadObjectScript easyRoad = roadGO.AddComponent<RoadObjectScript>();
        easyRoad.OOQDOOQQ = false;
        easyRoad.autoUpdate = true;
        easyRoad.surrounding = 3.0f;
        easyRoad.indent = 3.0f;
        easyRoad.geoResolution = 2.5f;
        easyRoad.objectType = 0;
        easyRoad.materialType = 0;
        easyRoad.multipleTerrains = true;
        easyRoad.ODQQQQQO = new string[0];
        easyRoad.ODDODDCCDCs = new GameObject[0];

        RealWorldTerrainUtils.CreateGameObject(roadGO, "SideObjects");
        GameObject markers = RealWorldTerrainUtils.CreateGameObject(roadGO, "Markers");

        easyRoad.OQOQCQOOOQ(easyRoad.transform, null, null, null);

        for (int i = 0; i < points.Count; i++)
        {
            string id = "000" + (i + 1);
            id = id.Substring(id.Length - 4, 4);
            GameObject go = (GameObject)Object.Instantiate(Resources.Load("marker", typeof(GameObject)));
            go.name = "Marker" + id;
            go.transform.position = points[i] + roadContainer.transform.position;
            go.transform.parent = markers.transform;
            MarkerScript scr = go.GetComponent<MarkerScript>();
            scr.OCOCQCQODD = false;
            scr.objectScript = easyRoad;
            if (i > 0) easyRoad.OCOQDDCCDC(easyRoad.geoResolution, false, false);
        }
#endif
    }

    private void CreateRoadArchitectRoad()
    {
#if ROADARCHITECT
        if (points.Count < 2) return;

        roadGo = tRoadSystem.AddRoad();
        roadGo.AddComponent<RealWorldTerrainMeta>().GetFromOSM(way);
        GSDRoad road = roadGo.GetComponent<GSDRoad>();
        road.opt_HeightModEnabled = false;
        road.opt_bShouldersEnabled = type == "primary";
        road.opt_DetailModEnabled = false;
        road.opt_bMaxGradeEnabled = false;
        road.opt_TreeModEnabled = false;

        if (type == "residential")
        {
            road.opt_LaneWidth = 2;
        }

        if (way.HasTagKey("surface"))
        {
            string surface = way.GetTagValue("surface");
            if (surface == "unpaved")
            {
                road.opt_tRoadMaterialDropdown = GSDRoad.RoadMaterialDropdownEnum.Dirt;
                road.opt_LaneWidth = 2.5f;
            }
        }

        if (way.HasTagKey("tracktype"))
        {
            road.opt_tRoadMaterialDropdown = GSDRoad.RoadMaterialDropdownEnum.Dirt;
            road.opt_LaneWidth = 2.5f;
        }

        road.transform.position = firstPoint;

        Vector3 offset = new Vector3(0, 0.5f, 0);
        splines = new List<GSDSplineN>();

        for (int i = 0; i < points.Count; i++)
        {
            Vector3 point = points[i];
            GameObject tNodeObj = new GameObject("Node" + i);
            GSDSplineN tNode = tNodeObj.AddComponent<GSDSplineN>();
            tNodeObj.AddComponent<RealWorldTerrainRoadArchitectNode>();
            tNodeObj.transform.position = point + offset + roadContainer.transform.position;
            tNodeObj.transform.parent = road.GSDSplineObj.transform;
            tNode.idOnSpline = i;
            tNode.GSDSpline = road.GSDSpline;
            tNode.bNeverIntersect = true;
            splines.Add(tNode);
        }

        road.UpdateRoad();
#endif
    }

    private void CreateSplineBendRoad()
    {
#if SPLINEBEND
        if (points.Count < 2) return;

        GameObject roadGO = RealWorldTerrainUtils.CreateGameObject(roadContainer, way.id);
        roadGO.AddComponent<RealWorldTerrainMeta>().GetFromOSM(way);
        roadGO.transform.position = firstPoint;
        roadGO.AddComponent<MeshRenderer>().sharedMaterial = prefs.splineBendMaterial;
        MeshFilter meshFilter = roadGO.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = prefs.splineBendMesh ?? new Mesh {name = "Mesh"};
        roadGO.AddComponent<MeshCollider>().sharedMesh = meshFilter.sharedMesh;
        SplineBend splineBend = roadGO.AddComponent<SplineBend>();
        
        int counter = 0;
        while (splineBend.transform.childCount > 0)
        {
            Object.DestroyImmediate(splineBend.transform.GetChild(0).gameObject);
            counter++;
            if (counter > 1000)
            {
                Debug.Log("Stop");
                break;
            }
        }

        float distance = 0;
        Vector3 lastPoint = Vector3.zero;

        List<SplineBendMarker> markers = new List<SplineBendMarker>();
        for (int i = 0; i < points.Count; i++)
        {
            GameObject markerGO = RealWorldTerrainUtils.CreateGameObject(roadGO, "Marker" + i);
            markerGO.transform.position = points[i];
            SplineBendMarker marker = markerGO.AddComponent<SplineBendMarker>();
            marker.position = points[i] + roadContainer.transform.position;
            markers.Add(marker);

            if (lastPoint != Vector3.zero) distance += (marker.position - lastPoint).magnitude;
            lastPoint = marker.position;
        }

        splineBend.markers = markers.ToArray();
        splineBend.tiles = Mathf.Max(Mathf.RoundToInt(distance / 2), 1);

        for (int i = 0; i < markers.Count; i++) markers[i].Init(splineBend, i);
        splineBend.dropToTerrain = true;
        splineBend.ForceUpdate();
#endif
    }

    public static void Generate(RealWorldTerrainContainer container)
    {
        if (!loaded)
        {
            Load();
            if (ways.Count == 0)
            {
                RealWorldTerrain.phaseComplete = true;
                return;
            }

            foreach (RealWorldTerrainOSMWay way in ways) new RealWorldTerrainOSMRoad(way, container);

            CombineRoads();

            if (RealWorldTerrain.generateTarget is RealWorldTerrainItem)
            {
                RealWorldTerrainItem item = RealWorldTerrain.generateTarget as RealWorldTerrainItem;
                roadContainer = RealWorldTerrainUtils.CreateGameObject(container, "Roads " + item.x + "x" + (item.container.terrainCount.y - item.y - 1));
                roadContainer.transform.position = item.transform.position;
            }
            else roadContainer = RealWorldTerrainUtils.CreateGameObject(container, "Roads");

            alreadyCreated = new List<string>();
#if ROADARCHITECT
            tRoadSystem = roadContainer.AddComponent<GSDRoadSystem>();
            RealWorldTerrainUtils.CreateGameObject(roadContainer, "Intersections");
#endif
#if EASYROADS
            RoadObjectScript.ODODQOOQ = new string[0];
#endif
        }

        long startTime = DateTime.Now.Ticks;

        for (int i = RealWorldTerrain.phaseIndex; i < roads.Count; i++)
        {
            RealWorldTerrainOSMRoad road = roads[i];
            if (alreadyCreated.Contains(road.id)) continue;
            alreadyCreated.Add(road.id);
            if (prefs.roadEngine == "Road Architect") road.CreateRoadArchitectRoad();
            else if (prefs.roadEngine == "SplineBend") road.CreateSplineBendRoad();
            else if (prefs.roadEngine == "EasyRoads3D") road.CreateEasyRoadsRoad();

            if (new TimeSpan(DateTime.Now.Ticks - startTime).TotalSeconds > 1)
            {
                RealWorldTerrain.phaseIndex = i + 1;
                RealWorldTerrain.phaseProgress = RealWorldTerrain.phaseIndex / (float)roads.Count;
                return;
            }
        }

        Dispose();
        GC.Collect();
        RealWorldTerrain.phaseComplete = true;
    }

    private static void Load()
    {
        if (prefs.roadTypes == 0) return;
        RealWorldTerrainOSM.LoadOSM(compressedFilename, out nodes, out ways, out relations);
        loaded = true;
    }

    private void NormalizeDistance()
    {
        int i = 0;
        while (i < points.Count - 1)
        {
            Vector3 p1 = points[i];
            Vector3 p2 = points[i + 1];
            if ((p1 - p2).magnitude < 10)
            {
                points[i] = Vector3.Lerp(p1, p2, 0.5f);
                points.RemoveAt(i + 1);
            }
            else i++;
        }

        i = 0;
        while (i < points.Count - 1)
        {
            Vector3 p1 = points[i];
            Vector3 p2 = points[i + 1];
            if ((p1 - p2).magnitude > 40) points.Insert(i + 1, Vector3.Lerp(p1, p2, 0.5f));
            else i++;
        }
    }

    private void NormalizePoints()
    {
        List<Vector3> newPoints = new List<Vector3>();
        newPoints.AddRange(points.Splice(0));
        Vector3 lastPoint = newPoints[0];

        while (points.Count > 0)
        {
            Vector3 closestPoint = Vector3.zero;
            float closestDistance = Single.MaxValue;
            foreach (Vector3 point in points)
            {
                float distance = (point - lastPoint).magnitude;
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPoint = point;
                }
            }

            newPoints.Add(closestPoint);
            lastPoint = closestPoint;
            points.Remove(closestPoint);
        }

        points = newPoints;
    }

    private static void OnDownloadComplete(ref byte[] data)
    {
        RealWorldTerrainOSM.GenerateCompressedFile(data, ref nodes, ref ways, ref relations, compressedFilename);
    }

    private void TrimPoints()
    {
        int index = 0;
        while (index < points.Count)
        {
            Vector3 p = points[index];
            if (p.x < 0 || p.z < 0 || p.x > container.size.x || p.z > container.size.z) points.RemoveAt(index);
            else index++;
        }
    }
}