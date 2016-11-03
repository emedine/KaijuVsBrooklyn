/*     INFINITY CODE 2013-2015      */
/*   http://www.infinity-code.com   */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class RealWorldTerrainPrefs
{
    private const string prefix = "RWT_";

    public bool allowChange = true;
    public int baseMapResolution = 1024;
    public int buildingGenerator = 0;
    public RealWorldTerrainRangeI buildingLevelLimits = new RealWorldTerrainRangeI(1, 50, 1, 50);
    public RealWorldTerrainBuildRCollider buildRCollider = RealWorldTerrainBuildRCollider.none;
    public RealWorldTerrainBuildRRenderMode buildRRenderMode = RealWorldTerrainBuildRRenderMode.full;
    public Vector2 coordinatesFrom = new Vector2(-113.6438f, 36.0358f);
    public Vector2 coordinatesTo = new Vector2(-113.0670f, 35.5680f);
    public int customBuildRGeneratorStyle = 0;
    public int customBuildRGeneratorTexturePack = 0;
    public RealWorldTerrainBuildRPresetsItem[] customBuildRPresets;
    public float depthSharpness = 1;
    public int detailResolution = 2048;
    public RealWorldTerrainElevationProvider elevationProvider = RealWorldTerrainElevationProvider.SRTM;
    public bool generateBuildings;
    public bool generateGrass;
    public bool generateRivers;
    public bool generateRoads;
    public bool generateTextures = false;
    public bool generateTrees;
    public int grassDensity = 100;
    public List<Texture2D> grassPrefabs;
    public int heightmapResolution = 129;
    public RealWorldTerrainMaxElevation maxElevationType = RealWorldTerrainMaxElevation.autoDetect;
    public int maxTextureLevel;
    public short nodataValue;
    public List<RealWorldTerrainPOI> POI;
    public bool reduceTextures = true;
    public int resolutionPerPatch = 16;
    public RealWorldTerrainResultType resultType = RealWorldTerrainResultType.terrain;
    public string roadEngine;
    public RealWorldTerrainOSMRoadType roadTypes = (RealWorldTerrainOSMRoadType)(-1);
    public int sizeType = 0;
    public Material splineBendMaterial;
    public Mesh splineBendMesh;
    public RealWorldTerrainVector2i terrainCount = RealWorldTerrainVector2i.one;
    public Vector3 terrainScale = Vector3.one;
    public RealWorldTerrainVector2i textureCount;
    public RealWorldTerrainTextureProvider textureProvider = RealWorldTerrainTextureProvider.virtualEarth;
    public string textureProviderURL = "http://localhost/tiles/{zoom}/{x}/{y}";
    public RealWorldTerrainVector2i textureSize = new RealWorldTerrainVector2i(1024, 1024);
    public RealWorldTerrainTextureType textureType = RealWorldTerrainTextureType.satellite;
    public int treeDensity = 100;
    public List<GameObject> treePrefabs;
    public List<RealWorldTerrainBuildingMaterial> buildingMaterials;

    public bool requiredOSM
    {
        get { return generateBuildings || generateGrass || generateRivers || generateTrees; }
    }

    public void Apply(RealWorldTerrainMonoBase target)
    {
        target.baseMapResolution = baseMapResolution;
        target.buildingGenerator = buildingGenerator;
        target.buildingMaterials = buildingMaterials;
        target.buildingLevelLimits = buildingLevelLimits;
        target.buildRCollider = buildRCollider;
        target.buildRRenderMode = buildRRenderMode;
        target.customBuildRGeneratorStyle = customBuildRGeneratorStyle;
        target.customBuildRGeneratorTexturePack = customBuildRGeneratorTexturePack;
        target.customBuildRPresets = customBuildRPresets;
        target.detailResolution = detailResolution;
        target.elevationProvider = elevationProvider;
        target.generateTextures = generateTextures;
        target.grassDensity = grassDensity;
        target.grassPrefabs = grassPrefabs;
        target.depthSharpness = depthSharpness;
        target.heightmapResolution = heightmapResolution;
        target.maxTextureLevel = maxTextureLevel;
        target.nodataValue = nodataValue;
        target.POI = POI;
        target.resolutionPerPatch = resolutionPerPatch;
        target.reduceTextures = reduceTextures;
        target.resultType = resultType;
        target.roadEngine = roadEngine;
        target.roadTypes = roadTypes;
        target.sizeType = sizeType;
        target.splineBendMaterial = splineBendMaterial;
        target.splineBendMesh = splineBendMesh;
        target.terrainScale = terrainScale;
        target.textureCount = textureCount;
        target.textureProvider = textureProvider;
        target.textureProviderURL = textureProviderURL;
        target.textureSize = textureSize;
        target.textureType = textureType;
        target.treeDensity = treeDensity;
        target.treePrefabs = treePrefabs;
    }

    public static void DeletePref(string id)
    {
        EditorPrefs.DeleteKey(prefix + id);
    }

    public static RealWorldTerrainPrefs GetPrefs(RealWorldTerrainMonoBase item, bool isNew = false)
    {
        RealWorldTerrainPrefs prefs = new RealWorldTerrainPrefs
        {
            baseMapResolution = item.baseMapResolution,
            buildingGenerator = item.buildingGenerator,
            buildingMaterials = item.buildingMaterials,
            buildingLevelLimits = item.buildingLevelLimits,
            buildRCollider = item.buildRCollider,
            buildRRenderMode = item.buildRRenderMode,
            coordinatesFrom = item.topLeft,
            coordinatesTo = item.bottomRight,
            customBuildRGeneratorStyle = item.customBuildRGeneratorStyle,
            customBuildRGeneratorTexturePack = item.customBuildRGeneratorTexturePack,
            customBuildRPresets = item.customBuildRPresets,
            depthSharpness = item.depthSharpness,
            detailResolution = item.detailResolution,
            elevationProvider = item.elevationProvider,
            generateTextures = item.generateTextures,
            grassDensity = item.grassDensity,
            grassPrefabs = item.grassPrefabs,
            heightmapResolution = item.heightmapResolution,
            maxTextureLevel = item.maxTextureLevel,
            nodataValue = item.nodataValue,
            POI = item.POI,
            resolutionPerPatch = item.resolutionPerPatch,
            reduceTextures = item.reduceTextures,
            resultType = item.resultType,
            roadEngine = item.roadEngine,
            roadTypes = item.roadTypes,
            sizeType = item.sizeType,
            splineBendMaterial = item.splineBendMaterial,
            splineBendMesh = item.splineBendMesh,
            terrainScale = item.terrainScale,
            textureCount = item.textureCount,
            textureProvider = item.textureProvider,
            textureProviderURL = item.textureProviderURL,
            textureSize = item.textureSize,
            textureType = item.textureType,
            treeDensity = item.treeDensity,
            treePrefabs = item.treePrefabs
        };

        if (!isNew)
        {
            prefs.allowChange = false;

            if (item is RealWorldTerrainContainer) prefs.terrainCount = ((RealWorldTerrainContainer)item).terrainCount;
            else prefs.terrainCount = RealWorldTerrainVector2i.one;
        }

        return prefs;
    }

    public void Load()
    {
        if (!allowChange) return;

        baseMapResolution = LoadPref("BaseMapRes", baseMapResolution);
        buildingGenerator = LoadPref("BuildingGenerator", 0);
        buildingLevelLimits = LoadPref("BuildingLevelLimits", buildingLevelLimits);
        buildRCollider = (RealWorldTerrainBuildRCollider)LoadPref("BuildRCollider", (int) RealWorldTerrainBuildRCollider.none);
        buildRRenderMode = (RealWorldTerrainBuildRRenderMode)LoadPref("BuildRRenderMode", (int)RealWorldTerrainBuildRRenderMode.full);
        coordinatesFrom = LoadPref("TL", coordinatesFrom);
        coordinatesTo = LoadPref("BR", coordinatesTo);
        detailResolution = LoadPref("DetailMapRes", detailResolution);
        elevationProvider = (RealWorldTerrainElevationProvider)LoadPref("ElevationProvider", (int)elevationProvider);
        generateBuildings = LoadPref("GenerateBuildings", generateBuildings);
        generateGrass = LoadPref("GenerateGrass", generateGrass);
        generateRivers = LoadPref("GenerateRivers", generateRivers);
        generateRoads = LoadPref("GenerateRoads", generateRoads);
        generateTextures = LoadPref("GenerateTextures", generateTextures);
        generateTrees = LoadPref("GenerateTrees", generateTrees);
        grassDensity = LoadPref("GrassDensity", 100);
        grassPrefabs = LoadPref("GrassPrefabs", grassPrefabs);
        heightmapResolution = LoadPref("HeightMapRes", heightmapResolution);
        maxElevationType = (RealWorldTerrainMaxElevation) LoadPref("MaxElevationType", (int) maxElevationType);
        maxTextureLevel = LoadPref("TextureLevel", maxTextureLevel);
        nodataValue = (short) LoadPref("NoDataValue", 0);
        depthSharpness = LoadPref("DepthSharpness", 1f);
        reduceTextures = LoadPref("ReduceTextures", reduceTextures);
        resolutionPerPatch = LoadPref("ResPerPatch", resolutionPerPatch);
        resultType = (RealWorldTerrainResultType)LoadPref("ResultType", (int)RealWorldTerrainResultType.terrain);
        roadEngine = LoadPref("RoadEngine", roadEngine);
        roadTypes = (RealWorldTerrainOSMRoadType)LoadPref("RoadTypes", -1);
        sizeType = LoadPref("SizeType", 0);
        textureCount = LoadPref("TextureCount", RealWorldTerrainVector2i.one);
        textureProvider = (RealWorldTerrainTextureProvider) LoadPref("TextureProvider", (int) textureProvider);
        textureProviderURL = LoadPref("TextureProviderURL", textureProviderURL);
        textureType = (RealWorldTerrainTextureType) LoadPref("TextureType", (int) textureType);
        textureSize = LoadPref("TextureSize", new RealWorldTerrainVector2i(1024, 1024));
        terrainCount = LoadPref("Count", RealWorldTerrainVector2i.one);
        terrainScale = LoadPref("Scale", Vector3.one);
        treePrefabs = LoadPref("TreePrefabs", treePrefabs);
        treeDensity = LoadPref("TreeDensity", 100);

        int POICount = LoadPref("POICount", 0);
        POI = new List<RealWorldTerrainPOI>();
        for (int i = 0; i < POICount; i++)
        {
            string p = "POI_" + i + "_";
            POI.Add(new RealWorldTerrainPOI(LoadPref(p + "Title", ""), LoadPref(p + "X", 0f), LoadPref(p + "Y", 0f)));
        }

        int buildingMaterialCount = LoadPref("BuildingMaterialCount", 0);
        buildingMaterials = new List<RealWorldTerrainBuildingMaterial>();
        for (int i = 0; i < buildingMaterialCount; i++)
        {
            string p = "BuildingMaterial_" + i + "_";
            int wallID = LoadPref(p + "Wall", -1);
            int roofID = LoadPref(p + "Roof", -1);

            RealWorldTerrainBuildingMaterial m = new RealWorldTerrainBuildingMaterial();

            if (wallID != -1) m.wall = EditorUtility.InstanceIDToObject(wallID) as Material;
            if (roofID != -1) m.roof = EditorUtility.InstanceIDToObject(roofID) as Material;

            buildingMaterials.Add(m);
        }
    }

    private List<Texture2D> LoadPref(string id, List<Texture2D> defVals)
    {
        string key = prefix + id + "_Count";
        if (EditorPrefs.HasKey(key))
        {
            int count = EditorPrefs.GetInt(prefix + id + "_Count");
            List<Texture2D> retVal = new List<Texture2D>();
            for (int i = 0; i < count; i++)
                retVal.Add(EditorUtility.InstanceIDToObject(EditorPrefs.GetInt(prefix + id + "_" + i)) as Texture2D);
            return retVal;
        }
        return defVals;
    }

    private RealWorldTerrainRangeI LoadPref(string id, RealWorldTerrainRangeI defVal)
    {
        defVal.min = LoadPref(id + "Min", defVal.min);
        defVal.max = LoadPref(id + "Max", defVal.max);
        return defVal;
    }

    private float LoadPref(string id, float defVal)
    {
        string key = prefix + id;
        if (EditorPrefs.HasKey(key)) return EditorPrefs.GetFloat(key);
        return defVal;
    }

    public static bool LoadPref(string id, bool defVal)
    {
        string key = prefix + id;
        if (EditorPrefs.HasKey(key)) return EditorPrefs.GetBool(key);
        return defVal;
    }

    private int LoadPref(string id, int defVal)
    {
        string key = prefix + id;
        if (EditorPrefs.HasKey(key)) return EditorPrefs.GetInt(key);
        return defVal;
    }

    public static string LoadPref(string id, string defVal)
    {
        string key = prefix + id;
        if (EditorPrefs.HasKey(key)) return EditorPrefs.GetString(key);
        return defVal;
    }

    private Object LoadPref(string id, Object defVal)
    {
        string key = prefix + id;
        if (EditorPrefs.HasKey(key)) return EditorUtility.InstanceIDToObject(EditorPrefs.GetInt(key));
        return defVal;
    }

    private List<GameObject> LoadPref(string id, List<GameObject> defVals)
    {
        string key = prefix + id + "_Count";
        if (EditorPrefs.HasKey(key))
        {
            int count = EditorPrefs.GetInt(prefix + id + "_Count");
            List<GameObject> retVal = new List<GameObject>();
            for (int i = 0; i < count; i++)
                retVal.Add(EditorUtility.InstanceIDToObject(EditorPrefs.GetInt(prefix + id + "_" + i)) as GameObject);
            return retVal;
        }
        return defVals;
    }

    private RealWorldTerrainVector2i LoadPref(string id, RealWorldTerrainVector2i defVal)
    {
        string key = prefix + id;
        if (EditorPrefs.HasKey(key + "X"))
            return new RealWorldTerrainVector2i(EditorPrefs.GetInt(key + "X"), EditorPrefs.GetInt(key + "Y"));
        return defVal;
    }

    private Vector2 LoadPref(string id, Vector2 defVal)
    {
        string key = prefix + id;
        if (EditorPrefs.HasKey(key + "X"))
            return new Vector2(EditorPrefs.GetFloat(key + "X"), EditorPrefs.GetFloat(key + "Y"));
        return defVal;
    }

    private Vector3 LoadPref(string id, Vector3 defVal)
    {
        string key = prefix + id;
        if (EditorPrefs.HasKey(key + "X"))
        {
            return new Vector3(EditorPrefs.GetFloat(key + "X"), EditorPrefs.GetFloat(key + "Y"),
                EditorPrefs.GetFloat(key + "Z"));
        }
        return defVal;
    }

    public void Save()
    {
        if (!allowChange) return;

        SetPref("TL", coordinatesFrom);
        SetPref("BR", coordinatesTo);
        SetPref("Count", terrainCount);
        SetPref("Scale", terrainScale);
        SetPref("BaseMapRes", baseMapResolution);
        SetPref("DetailMapRes", detailResolution);
        SetPref("ElevationProvider", (int)elevationProvider);
        SetPref("HeightMapRes", heightmapResolution);
        SetPref("ResPerPatch", resolutionPerPatch);
        SetPref("GenerateBuildings", generateBuildings);
        SetPref("GenerateGrass", generateGrass);
        SetPref("GenerateRivers", generateRivers);
        SetPref("GenerateRoads", generateRoads);
        SetPref("GenerateTextures", generateTextures);
        SetPref("GenerateTrees", generateTrees);
        SetPref("GrassDensity", grassDensity);
        SetPref("GrassPrefabs", grassPrefabs);
        SetPref("MaxElevationType", (int) maxElevationType);
        SetPref("NoDataValue", nodataValue);
        SetPref("DepthSharpness", depthSharpness);
        SetPref("ReduceTextures", reduceTextures);
        SetPref("ResultType", (int)resultType);
        SetPref("RoadEngine", roadEngine);
        SetPref("RoadTypes", (int)roadTypes);
        SetPref("SizeType", sizeType);
        SetPref("TextureCount", textureCount);
        SetPref("TextureProvider", (int) textureProvider);
        SetPref("TextureProviderURL", textureProviderURL);
        SetPref("TextureType", (int) textureType);
        SetPref("TextureLevel", maxTextureLevel);
        SetPref("TextureSize", textureSize);
        SetPref("TreeDensity", treeDensity);
        SetPref("TreePrefabs", treePrefabs);
        SetPref("BuildingGenerator", buildingGenerator);
        SetPref("BuildingLevelLimits", buildingLevelLimits);
        SetPref("BuildRCollider", (int)buildRCollider);
        SetPref("BuildRRenderMode", (int)buildRRenderMode);

        string coordsScript = "var Coords = {" + String.Format("tlx: {0}, tly: {1}, brx: {2}, bry: {3}", coordinatesFrom.x, coordinatesFrom.y, coordinatesTo.x, coordinatesTo.y) + "};";

        if (POI != null)
        {
            coordsScript += "var POI = [";

            SetPref("POICount", POI.Count);
            for (int i = 0; i < POI.Count; i++)
            {
                string p = "POI_" + i + "_";
                RealWorldTerrainPOI poi = POI[i];
                SetPref(p + "Title", poi.title);
                SetPref(p + "X", poi.x);
                SetPref(p + "Y", poi.y);
                if (i > 0) coordsScript += ", ";
                coordsScript += "{x: " + poi.x + ", y:" + poi.y + ", title: \"" + poi.title + "\"}";
            }

            coordsScript += "];";
        }
        else
        {
            SetPref("POICount", 0);
        }

        if (buildingMaterials != null)
        {
            SetPref("BuildingMaterialCount", buildingMaterials.Count);
            for (int i = 0; i < buildingMaterials.Count; i++)
            {
                string p = "BuildingMaterial_" + i + "_";
                if (buildingMaterials[i].wall != null) SetPref(p + "Wall", buildingMaterials[i].wall.GetInstanceID());
                else SetPref(p + "Wall", -1);

                if (buildingMaterials[i].roof != null) SetPref(p + "Roof", buildingMaterials[i].roof.GetInstanceID());
                else SetPref(p + "Roof", -1);
            }
        }
        else
        {
            SetPref("BuildingMaterialCount", 0);
        }

        string coordPath =
            Directory.GetFiles(Application.dataPath, "RWT_Coords.jscript", SearchOption.AllDirectories)[0].Replace(
                '\\', '/');
        File.WriteAllText(coordPath, coordsScript);
    }

    private void SetPref(string id, List<Texture2D> vals)
    {
        if (vals != null)
        {
            EditorPrefs.SetInt(prefix + id + "_Count", vals.Count);

            for (int i = 0; i < vals.Count; i++)
            {
                Object val = vals[i];
                if (val != null) EditorPrefs.SetInt(prefix + id + "_" + i, val.GetInstanceID());
            }
        }
        else EditorPrefs.SetInt(prefix + id + "_Count", 0);
    }

    private void SetPref(string id, RealWorldTerrainRangeI val)
    {
        SetPref(id + "Min", val.min);
        SetPref(id + "Max", val.max);
    }

    private void SetPref(string id, float val)
    {
        EditorPrefs.SetFloat(prefix + id, val);
    }

    public static void SetPref(string id, bool val)
    {
        EditorPrefs.SetBool(prefix + id, val);
    }

    private void SetPref(string id, int val)
    {
        EditorPrefs.SetInt(prefix + id, val);
    }

    private void SetPref(string id, Object val)
    {
        if (val != null) EditorPrefs.SetInt(prefix + id, val.GetInstanceID());
    }

    private void SetPref(string id, List<GameObject> vals)
    {
        if (vals != null)
        {
            EditorPrefs.SetInt(prefix + id + "_Count", vals.Count);

            for (int i = 0; i < vals.Count; i++)
            {
                Object val = vals[i];
                if (val != null) EditorPrefs.SetInt(prefix + id + "_" + i, val.GetInstanceID());
            }
        }
        else EditorPrefs.SetInt(prefix + id + "_Count", 0);
    }

    private void SetPref(string id, RealWorldTerrainVector2i val)
    {
        EditorPrefs.SetInt(prefix + id + "X", val.x);
        EditorPrefs.SetInt(prefix + id + "Y", val.y);
    }

    public static void SetPref(string id, string val)
    {
        EditorPrefs.SetString(prefix + id, val);
    }

    private void SetPref(string id, Vector2 val)
    {
        EditorPrefs.SetFloat(prefix + id + "X", val.x);
        EditorPrefs.SetFloat(prefix + id + "Y", val.y);
    }

    private void SetPref(string id, Vector3 val)
    {
        EditorPrefs.SetFloat(prefix + id + "X", val.x);
        EditorPrefs.SetFloat(prefix + id + "Y", val.y);
        EditorPrefs.SetFloat(prefix + id + "Z", val.z);
    }

    public XmlNode ToXML(XmlDocument document)
    {
        XmlNode node = document.CreateElement("Prefs");

        node.CreateChild("BaseMapRes", baseMapResolution);
        node.CreateChild("TL", coordinatesFrom);
        node.CreateChild("BR", coordinatesTo);
        node.CreateChild("DetailMapRes", detailResolution);
        node.CreateChild("ElevationProvider", (int)elevationProvider);
        node.CreateChild("GenerateBuildings", generateBuildings);
        node.CreateChild("GenerateGrass", generateGrass);
        node.CreateChild("GenerateRivers", generateRivers);
        node.CreateChild("GenerateRoads", generateRoads);
        node.CreateChild("GenerateTextures", generateTextures);
        node.CreateChild("GenerateTrees", generateTrees);
        node.CreateChild("HeightMapRes", heightmapResolution);
        node.CreateChild("MaxElevationType", (int)maxElevationType);
        node.CreateChild("DepthSharpness", depthSharpness);
        node.CreateChild("NoDataValue", nodataValue);
        node.CreateChild("TextureLevel", maxTextureLevel);
        node.CreateChild("ResPerPatch", resolutionPerPatch);
        node.CreateChild("RoadEngine", roadEngine);
        node.CreateChild("RoadTypes", (int)roadTypes);
        node.CreateChild("ResultType", (int)resultType);
        node.CreateChild("SizeType", sizeType);
        node.CreateChild("TextureCount", textureCount);
        node.CreateChild("TextureProvider", (int)textureProvider);
        node.CreateChild("TextureProviderURL", textureProviderURL);
        node.CreateChild("TextureType", (int)textureType);
        node.CreateChild("TextureSize", textureSize);
        node.CreateChild("TreeDensity", treeDensity);
        node.CreateChild("Count", terrainCount);
        node.CreateChild("Scale", terrainScale);
        node.CreateChild("BuildingGenerator", buildingGenerator);
        node.CreateChild("BuildingLevelLimits", buildingLevelLimits);
        node.CreateChild("BuildRCollider", (int)buildRCollider);
        node.CreateChild("BuildRRenderMode", (int)buildRRenderMode);

        if (POI != null)
        {
            XmlNode poiNode = node.CreateChild("POIs");
            foreach (RealWorldTerrainPOI poi in POI)
            {
                XmlNode pNode = poiNode.CreateChild("POI");
                pNode.CreateChild("Title", poi.title);
                pNode.CreateChild("X", poi.x);
                pNode.CreateChild("Y", poi.y);
            }
        }

        return node;
    }
}