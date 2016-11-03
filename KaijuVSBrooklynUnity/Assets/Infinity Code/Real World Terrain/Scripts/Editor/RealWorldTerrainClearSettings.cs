using UnityEditor;

public static class RealWorldTerrainClearSettings
{
    [MenuItem("Window/Infinity Code/Real World Terrain/Clear Settings", false)]
    public static void Clear()
    {
        RealWorldTerrainPrefs.DeletePref("TL");
        RealWorldTerrainPrefs.DeletePref("BR");
        RealWorldTerrainPrefs.DeletePref("Count");
        RealWorldTerrainPrefs.DeletePref("Scale");
        RealWorldTerrainPrefs.DeletePref("BaseMapRes");
        RealWorldTerrainPrefs.DeletePref("DetailMapRes");
        RealWorldTerrainPrefs.DeletePref("ElevationProvider");
        RealWorldTerrainPrefs.DeletePref("HeightMapRes");
        RealWorldTerrainPrefs.DeletePref("ResPerPatch");
        RealWorldTerrainPrefs.DeletePref("GenerateBuildings");
        RealWorldTerrainPrefs.DeletePref("GenerateGrass");
        RealWorldTerrainPrefs.DeletePref("GenerateRivers");
        RealWorldTerrainPrefs.DeletePref("GenerateRoads");
        RealWorldTerrainPrefs.DeletePref("GenerateTextures");
        RealWorldTerrainPrefs.DeletePref("GenerateTrees");
        RealWorldTerrainPrefs.DeletePref("GrassDensity");
        RealWorldTerrainPrefs.DeletePref("GrassPrefabs");
        RealWorldTerrainPrefs.DeletePref("MaxElevationType");
        RealWorldTerrainPrefs.DeletePref("NoDataValue");
        RealWorldTerrainPrefs.DeletePref("DepthSharpness");
        RealWorldTerrainPrefs.DeletePref("ReduceTextures");
        RealWorldTerrainPrefs.DeletePref("ResultType");
        RealWorldTerrainPrefs.DeletePref("RoadEngine");
        RealWorldTerrainPrefs.DeletePref("RoadTypes");
        RealWorldTerrainPrefs.DeletePref("SizeType");
        RealWorldTerrainPrefs.DeletePref("TextureCount");
        RealWorldTerrainPrefs.DeletePref("TextureProvider");
        RealWorldTerrainPrefs.DeletePref("TextureProviderURL");
        RealWorldTerrainPrefs.DeletePref("TextureType");
        RealWorldTerrainPrefs.DeletePref("TextureLevel");
        RealWorldTerrainPrefs.DeletePref("TextureSize");
        RealWorldTerrainPrefs.DeletePref("TreeDensity");
        RealWorldTerrainPrefs.DeletePref("TreePrefabs");
        RealWorldTerrainPrefs.DeletePref("BuildingGenerator");
        RealWorldTerrainPrefs.DeletePref("BuildingLevelLimits");
        RealWorldTerrainPrefs.DeletePref("BuildRCollider");
        RealWorldTerrainPrefs.DeletePref("BuildRRenderMode");
        RealWorldTerrainPrefs.DeletePref("POICount");
        RealWorldTerrainPrefs.DeletePref("BuildingMaterialCount");
    }
}
