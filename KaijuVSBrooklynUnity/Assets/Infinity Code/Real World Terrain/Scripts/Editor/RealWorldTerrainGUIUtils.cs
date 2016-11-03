/*     INFINITY CODE 2013-2015      */
/*   http://www.infinity-code.com   */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEditor;
using UnityEngine;

public static class RealWorldTerrainGUIUtils
{
    public static int iProgress;
    public static string phasetitle = "";

    private static Vector2 scrollPos = Vector2.zero;
    private static bool showCoordinates = true;
    private static bool showBuildRCustomPresets;
    private static bool showCustomProviderTokens;
    private static bool showPOI;
    private static bool showT4MPerformances;
    private static bool showTerrains = true;
    private static bool showTextures = true;

    private static readonly string[] labels2n = { "32", "64", "128", "256", "512", "1024", "2048", "4096" };
    private static readonly int[] values2n = { 32, 64, 128, 256, 512, 1024, 2048, 4096 };
    private static readonly string[] labels2n1 = { "33", "65", "129", "257", "513", "1025", "2049", "4097" };
    private static readonly int[] values2n1 = { 33, 65, 129, 257, 513, 1025, 2049, 4097 };
    private static readonly string[] labels2n4 = { "4", "8", "16", "32", "64", "128", "256", "512", "1024", "2048", "4096" };
    private static readonly int[] values2n4 = { 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096 };

    public static RealWorldTerrainGenerateType generateType
    {
        get { return RealWorldTerrain.generateType; }
    }

    private static RealWorldTerrainPhase phase
    {
        get { return RealWorldTerrain._phase; }
    }

    private static RealWorldTerrainPrefs prefs
    {
        get { return RealWorldTerrain.prefs; }
    }

    private static RealWorldTerrain wnd
    {
        get { return RealWorldTerrain.wnd; }
    }

    private static void DrawHelpButton(string tooltip, string href = null)
    {
        if (GUILayout.Button(new GUIContent(RealWorldTerrainResources.helpIcon, tooltip),
            RealWorldTerrainResources.helpStyle, GUILayout.ExpandWidth(false)) && !string.IsNullOrEmpty(href))
            Application.OpenURL(href);
    }

    public static float FloatField(string label, float value, string tooltip, string href = "")
    {
        GUILayout.BeginHorizontal();
        value = EditorGUILayout.FloatField(label, value);

        if (GUILayout.Button(new GUIContent(RealWorldTerrainResources.helpIcon, tooltip),
            RealWorldTerrainResources.helpStyle, GUILayout.ExpandWidth(false)))
        {
            if (href != "") Application.OpenURL(href);
        }
        GUILayout.EndHorizontal();
        return value;
    }

    public static int IntPopup(string label, int value, string[] displayedOptions, int[] optionValues, string tooltip, string href)
    {
        GUILayout.BeginHorizontal();
        value = EditorGUILayout.IntPopup(label, value, displayedOptions, optionValues);

        DrawHelpButton(tooltip, href);
        GUILayout.EndHorizontal();
        return value;
    }

    public static int IntPopup(string label, int value, string[] displayedOptions, int[] optionValues, string tooltip, string[] hrefs)
    {
        GUILayout.BeginHorizontal();
        value = EditorGUILayout.IntPopup(label, value, displayedOptions, optionValues);
        DrawHelpButton(tooltip, hrefs[value]);
        GUILayout.EndHorizontal();
        return value;
    }

    public static int IntField(string label, int value, string tooltip, string href = "")
    {
        GUILayout.BeginHorizontal();
        value = EditorGUILayout.IntField(label, value);

        if (GUILayout.Button(new GUIContent(RealWorldTerrainResources.helpIcon, tooltip),
            RealWorldTerrainResources.helpStyle, GUILayout.ExpandWidth(false)))
        {
            if (href != "") Application.OpenURL(href);
        }
        GUILayout.EndHorizontal();
        return value;
    }

    private static void OnCaptureGUI()
    {
        if (phase == RealWorldTerrainPhase.downloading)
        {
            int completed = Mathf.FloorToInt(RealWorldTerrainDownloader.totalSizeMB * RealWorldTerrain.progress);
            GUILayout.Label(phasetitle + " (" + completed + " of " +
                            RealWorldTerrainDownloader.totalSizeMB + " mb)");
        }
        else GUILayout.Label(phasetitle);

        Rect r = EditorGUILayout.BeginVertical();
        iProgress = Mathf.FloorToInt(RealWorldTerrain.progress * 100);
        EditorGUI.ProgressBar(r, RealWorldTerrain.progress, iProgress + "%");
        GUILayout.Space(16);
        EditorGUILayout.EndVertical();

        if (GUILayout.Button("Cancel")) RealWorldTerrain.CancelCapture();

        GUILayout.Label("Warning: Keep this window open.");
    }

    private static void OnFeatureRequest()
    {
        RealWorldTerrainFeatureRequest.OpenWindow();
    }

    public static void OnGUI()
    {
#if !UNITY_WEBPLAYER
        if (!RealWorldTerrain.isCapturing) OnIdleGUI();
        else OnCaptureGUI();
#else
        OnWebplayerWarning();
#endif
    }

    private static void OnIdleCoordinatesGUI()
    {
        GUILayout.Label("Top-Left: ");
        prefs.coordinatesFrom.y = FloatField("Latitude: ", prefs.coordinatesFrom.y, "Latitude of the Top-Left corner of the area. \nValues: -60 to 60.", "http://en.wikipedia.org/wiki/Latitude");
        prefs.coordinatesFrom.x = FloatField("Longitude: ", prefs.coordinatesFrom.x, "Longitude of the Top-Left corner of the area. \nValues: -180 to 180.", "http://en.wikipedia.org/wiki/Longitude");
        GUILayout.Space(10);

        GUILayout.Label("Bottom-Right: ");
        prefs.coordinatesTo.y = FloatField("Latitude: ", prefs.coordinatesTo.y, "Latitude of the Bottom-Right corner of the area. \nValues: -60 to 60.", "http://en.wikipedia.org/wiki/Latitude");
        prefs.coordinatesTo.x = FloatField("Longitude: ", prefs.coordinatesTo.x, "Longitude of the Bottom-Right corner of the area. \nValues: -180 to 180.", "http://en.wikipedia.org/wiki/Longitude");

        GUILayout.Space(10);

        GUI.SetNextControlName("InsertCoordsButton");
        if (GUILayout.Button("Insert the coordinates from the clipboard")) OnInsertCoords();
        if (GUILayout.Button("Run the helper")) OnRunHelper();
        if (prefs.resultType == RealWorldTerrainResultType.terrain && GUILayout.Button("Get the best settings for the specified coordinates")) RealWorldTerrainSettingsGenerator.OpenWindow();

        GUILayout.Space(10);
    }

    private static void OnIdleGUI()
    {
        OnIdleGUIToolbar();
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        if (RealWorldTerrain.generateType == RealWorldTerrainGenerateType.full)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            showCoordinates = EditorGUILayout.Foldout(showCoordinates, "Decimal coordinates");
            if (showCoordinates) OnIdleCoordinatesGUI();
            EditorGUILayout.EndVertical();
        }

        if (RealWorldTerrain.generateType == RealWorldTerrainGenerateType.full)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            showTerrains = EditorGUILayout.Foldout(showTerrains, "Terrains");
        }
            
        if (showTerrains) OnIdleTerrainGUI();

        if (RealWorldTerrain.generateType == RealWorldTerrainGenerateType.full) EditorGUILayout.EndVertical();

        if (RealWorldTerrain.generateType == RealWorldTerrainGenerateType.full || RealWorldTerrain.generateType == RealWorldTerrainGenerateType.texture)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.BeginHorizontal();
            if (prefs.generateTextures) showTextures = GUILayout.Toggle(showTextures, "", EditorStyles.foldout, GUILayout.ExpandWidth(false));
            prefs.generateTextures =
                GUILayout.Toggle(prefs.generateTextures, "Textures");
            EditorGUILayout.EndHorizontal();
            if (showTextures && prefs.generateTextures) OnIdleTextureGUI();
            EditorGUILayout.EndVertical();
        }

        if (RealWorldTerrain.generateType == RealWorldTerrainGenerateType.full)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.BeginHorizontal();
            showPOI = EditorGUILayout.Foldout(showPOI, "POI");
            DrawHelpButton("Here you can specify a point of interest, which will be created on the terrains.");
            EditorGUILayout.EndHorizontal();
            if (showPOI) OnIdleGUIPOI();
            EditorGUILayout.EndVertical();
        }

        OnIdleOSMGUI();

        GUILayout.EndScrollView();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Start")) RealWorldTerrain.StartCapture();
        if (GUILayout.Button("Clear cache", GUILayout.ExpandWidth(false))) RealWorldTerrain.ClearCache();
        GUILayout.EndHorizontal();
    }

    private static void OnIdleGUIPOI()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (prefs.POI == null) prefs.POI = new List<RealWorldTerrainPOI>();

        if (GUILayout.Button(new GUIContent("+", "Add POI"), EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
        {
            prefs.POI.Add(new RealWorldTerrainPOI("New POI " + (prefs.POI.Count + 1),
                (prefs.coordinatesFrom.x + prefs.coordinatesTo.x) / 2, (prefs.coordinatesFrom.x + prefs.coordinatesTo.y) / 2));
        }

        GUILayout.Label("");

        if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false))) prefs.POI = new List<RealWorldTerrainPOI>();

        EditorGUILayout.EndHorizontal();

        if (prefs.POI.Count == 0)
        {
            GUILayout.Label("POI is not specified.");
        }

        int poiIndex = 1;
        int poiDeleteIndex = -1;

        foreach (RealWorldTerrainPOI poi in prefs.POI)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(poiIndex.ToString(), GUILayout.ExpandWidth(false));
            poi.title = EditorGUILayout.TextField("", poi.title);
            GUILayout.Label("Lat:", GUILayout.ExpandWidth(false));
            poi.y = EditorGUILayout.FloatField("", poi.y, GUILayout.Width(80));
            GUILayout.Label("Lng:", GUILayout.ExpandWidth(false));
            poi.x = EditorGUILayout.FloatField("", poi.x, GUILayout.Width(80));
            if (GUILayout.Button(new GUIContent("X", "Delete POI"), GUILayout.ExpandWidth(false))) poiDeleteIndex = poiIndex - 1;
            EditorGUILayout.EndHorizontal();

            poiIndex++;
        }

        if (poiDeleteIndex != -1) prefs.POI.RemoveAt(poiDeleteIndex);

        EditorGUILayout.Space();
    }

    private static void OnIdleGUIToolbar()
    {
        GUIStyle buttonStyle = new GUIStyle(EditorStyles.toolbarButton);

        GUILayout.BeginHorizontal();

        if (RealWorldTerrainUpdater.hasNewVersion)
        {
            Color defColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1, 0.5f, 0.5f);
            if (GUILayout.Button("New version available!!! Click here to update.", buttonStyle))
            {
                wnd.Close();
                RealWorldTerrainUpdater.OpenWindow();
            }
            GUI.backgroundColor = defColor;
        }
        else
            GUILayout.Label("", buttonStyle);

        if (GUILayout.Button("Settings", buttonStyle, GUILayout.ExpandWidth(false))) RealWorldTerrainSettings.OpenWindow();

        if (GUILayout.Button("Help", buttonStyle, GUILayout.ExpandWidth(false)))
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Online Documentation"), false, OnViewDocs);
            menu.AddItem(new GUIContent("Product Page"), false, OnProductPage);
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Bug Report"), false, OnReportError);
            menu.AddItem(new GUIContent("Feature Request"), false, OnFeatureRequest);
            menu.AddItem(new GUIContent("Support"), false, OnSendMail);
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Check Updates"), false, RealWorldTerrainUpdater.OpenWindow);
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("About"), false, RealWorldTerrainAboutWindow.OpenWindow);
            menu.ShowAsContext();
        }

        GUILayout.EndHorizontal();

        EditorGUILayout.Space();
    }

    private static void OnIdleOSMGUI()
    {
        if (RealWorldTerrain.generateType != RealWorldTerrainGenerateType.full && RealWorldTerrain.generateType != RealWorldTerrainGenerateType.additional)
            return;

        OnIdleOSMGUIBuildings();
        OnIdleOSMGUIRoads();

        EditorGUILayout.BeginVertical(GUI.skin.box);
        prefs.generateRivers = EditorGUILayout.Toggle("Generate rivers", prefs.generateRivers);
        EditorGUILayout.EndVertical();

        if (prefs.resultType == RealWorldTerrainResultType.terrain)
        {
            OnIdleOSMGUITrees();
            OnIdleOSMGUIGrass();
        }
        else
        {
            prefs.generateGrass = false;
            prefs.generateTrees = false;
        }
    }

    private static void OnIdleOSMGUIBuildings()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);
        prefs.generateBuildings = EditorGUILayout.Toggle("Generate buildings", prefs.generateBuildings);

        if (prefs.generateBuildings)
        {
            List<string> availableBuilders = new List<string> { "Built-in" };
#if BUILDR
            availableBuilders.Add("BuildR");
#endif
            if (availableBuilders.Count > 1)
            {
                prefs.buildingGenerator = EditorGUILayout.Popup("Building generator:", prefs.buildingGenerator,
                    availableBuilders.ToArray());
            }
            else prefs.buildingGenerator = 0;

            if (availableBuilders[prefs.buildingGenerator] == "BuildR")
            {
                if (GUILayout.Button("BuildR presets")) RealWorldTerrainBuildRPresets.OpenWindow();
                prefs.buildRCollider = (RealWorldTerrainBuildRCollider)EditorGUILayout.EnumPopup("Collider", prefs.buildRCollider);
                prefs.buildRRenderMode = (RealWorldTerrainBuildRRenderMode)EditorGUILayout.EnumPopup("Render Mode", prefs.buildRRenderMode);
            }

            RealWorldTerrainRangeI range = prefs.buildingLevelLimits;
            float minLevelLimit = range.min;
            float maxLevelLimit = range.max;
            EditorGUILayout.MinMaxSlider(
                new GUIContent(string.Format("Levels if unknown ({0}-{1})", range.min, range.max)), ref minLevelLimit,
                ref maxLevelLimit, 1, 50);
            range.Set(minLevelLimit, maxLevelLimit);

            if (availableBuilders[prefs.buildingGenerator] == "Built-in")
            {
                GUILayout.Label("Building Materials:");
                if (prefs.buildingMaterials == null) prefs.buildingMaterials = new List<RealWorldTerrainBuildingMaterial>();
                for (int i = 0; i < prefs.buildingMaterials.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();

                    GUILayout.Label((i + 1).ToString(), GUILayout.ExpandWidth(false));

                    EditorGUILayout.BeginVertical();

                    RealWorldTerrainBuildingMaterial material = prefs.buildingMaterials[i];
                    material.wall = EditorGUILayout.ObjectField("Wall material: ", material.wall, typeof(Material), false) as Material;
                    material.roof = EditorGUILayout.ObjectField("Roof material: ", material.roof, typeof(Material), false) as Material;

                    EditorGUILayout.EndVertical();

                    if (GUILayout.Button("X", GUILayout.ExpandWidth(false)))
                    {
                        prefs.buildingMaterials[i] = null;
                    }

                    EditorGUILayout.EndHorizontal();
                }

                prefs.buildingMaterials.RemoveAll(m => m == null);

                if (GUILayout.Button("Add material")) prefs.buildingMaterials.Add(new RealWorldTerrainBuildingMaterial());
            }
        }
        EditorGUILayout.EndVertical();
    }

    private static void OnIdleOSMGUIGrass()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);
        prefs.generateGrass = EditorGUILayout.Toggle("Generate grass", prefs.generateGrass);
        if (prefs.generateGrass)
        {
            prefs.grassDensity = EditorGUILayout.IntField("Density (%):", prefs.grassDensity);
            if (prefs.grassDensity < 1) prefs.grassDensity = 1;
            if (prefs.grassPrefabs == null) prefs.grassPrefabs = new List<Texture2D>();

            EditorGUILayout.LabelField("Grass Prefabs:");
            for (int i = 0; i < prefs.grassPrefabs.Count; i++)
            {
                prefs.grassPrefabs[i] =
                    (Texture2D)
                        EditorGUILayout.ObjectField((i + 1) + ":", prefs.grassPrefabs[i], typeof(Texture2D), false);
            }
            Texture2D newGrass =
                (Texture2D)
                    EditorGUILayout.ObjectField((prefs.grassPrefabs.Count + 1) + ":", null, typeof(Texture2D), false);
            if (newGrass != null) prefs.grassPrefabs.Add(newGrass);
            prefs.grassPrefabs.RemoveAll(go => go == null);
            EditorGUILayout.Space();
        }
        EditorGUILayout.EndVertical();
    }

    private static void OnIdleOSMGUIRoads()
    {
        List<string> availableRoadType = new List<string>();
#if ROADARCHITECT
        availableRoadType.Add("Road Architect");
#endif
#if SPLINEBEND
        availableRoadType.Add("SplineBend");
#endif
#if EASYROADS
        availableRoadType.Add("EasyRoads3D");
#endif

        if (availableRoadType.Count == 0)
        {
            prefs.generateRoads = false;
            return;
        }

        EditorGUILayout.BeginVertical(GUI.skin.box);
        prefs.generateRoads = EditorGUILayout.Toggle("Generate roads", prefs.generateRoads);

        if (!prefs.generateRoads)
        {
            EditorGUILayout.EndVertical();
            return;
        }

        if (availableRoadType.Count > 1)
        {
            int roadEngineIndex = availableRoadType.IndexOf(prefs.roadEngine);
            if (roadEngineIndex == -1) roadEngineIndex = 0;
            roadEngineIndex = EditorGUILayout.Popup("Road engine: ", roadEngineIndex, availableRoadType.ToArray());
            prefs.roadEngine = availableRoadType[roadEngineIndex];
        }
        else prefs.roadEngine = availableRoadType[0];

        if (prefs.roadEngine == "SplineBend")
        {
            prefs.splineBendMaterial = (Material)EditorGUILayout.ObjectField("Material: ", prefs.splineBendMaterial,
                typeof(Material), false);
            prefs.splineBendMesh = (Mesh)EditorGUILayout.ObjectField("Mesh: ", prefs.splineBendMesh,
                typeof(Mesh), false);
        }

        prefs.roadTypes = (RealWorldTerrainOSMRoadType)EditorGUILayout.EnumMaskField("Road types:", prefs.roadTypes);
        EditorGUILayout.EndVertical();
    }

    private static void OnIdleOSMGUITrees()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);
        prefs.generateTrees = EditorGUILayout.Toggle("Generate trees", prefs.generateTrees);

        if (prefs.generateTrees)
        {
            prefs.treeDensity = EditorGUILayout.IntField("Density (%):", prefs.treeDensity);
            if (prefs.treeDensity < 1) prefs.treeDensity = 1;
            if (prefs.treePrefabs == null) prefs.treePrefabs = new List<GameObject>();
            EditorGUILayout.LabelField("Tree Prefabs:");
            for (int i = 0; i < prefs.treePrefabs.Count; i++)
            {
                prefs.treePrefabs[i] =
                    (GameObject)
                        EditorGUILayout.ObjectField((i + 1) + ":", prefs.treePrefabs[i], typeof(GameObject), false);
            }
            GameObject newTree =
                (GameObject)
                    EditorGUILayout.ObjectField((prefs.treePrefabs.Count + 1) + ":", null, typeof(GameObject), false);
            if (newTree != null) prefs.treePrefabs.Add(newTree);
            prefs.treePrefabs.RemoveAll(go => go == null);
            EditorGUILayout.Space();
        }
        EditorGUILayout.EndVertical();
    }

    private static void OnIdleTerrainGUI()
    {
        if (generateType != RealWorldTerrainGenerateType.full && generateType != RealWorldTerrainGenerateType.terrain)
            return;

        if (generateType == RealWorldTerrainGenerateType.full)
        {
            prefs.elevationProvider = (RealWorldTerrainElevationProvider)EditorGUILayout.EnumPopup("Elevation provider: ", prefs.elevationProvider);

            prefs.resultType = (RealWorldTerrainResultType)EditorGUILayout.EnumPopup("Result: ", prefs.resultType);

            if (prefs.resultType == RealWorldTerrainResultType.terrain)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Count terrains.    X: ");
                prefs.terrainCount.x = Mathf.Max(EditorGUILayout.IntField(prefs.terrainCount.x), 1);
                GUILayout.Label("Y: ");
                prefs.terrainCount.y = Mathf.Max(EditorGUILayout.IntField(prefs.terrainCount.y), 1);
                GUILayout.EndHorizontal();
            }

            prefs.sizeType = IntPopup("Size type:", prefs.sizeType, new[] { "Real world sizes", "Mercator sizes" }, new[] { 0, 1 },
                "Specifies whether the projection will be determined by the size of the area.",
                new[] { "http://en.wikipedia.org/wiki/Cylindrical_equal-area_projection", "http://en.wikipedia.org/wiki/Mercator_projection" });

            prefs.terrainScale = EditorGUILayout.Vector3Field("Scale", prefs.terrainScale);
            prefs.maxElevationType =
                (RealWorldTerrainMaxElevation)EditorGUILayout.EnumPopup("Max elevation: ", prefs.maxElevationType);
        }
        int nodata = IntField("Max underwater depth: ", prefs.nodataValue,
            "SRTM v4.1 does not contain data on the underwater depths. Real World Terrain generates it by closest known areas of land. \nSpecify a value relative to sea level. \nFor example, if you want to get a depth of 200 meters, set the value \"-200\".");
        if (nodata < short.MinValue) nodata = short.MinValue;
        if (nodata > short.MaxValue) nodata = short.MaxValue;
        prefs.depthSharpness = FloatField("Depth sharpness: ", prefs.depthSharpness, "Escarpment of the seabed. \nGreater value - steeper slope.");
        if (prefs.depthSharpness < 0) prefs.depthSharpness = 0;
        prefs.nodataValue = (short)nodata;
        if (prefs.resultType == RealWorldTerrainResultType.terrain)
        {
            prefs.detailResolution = IntField("Detail Resolution", prefs.detailResolution,
                "The resolution of the map that controls grass and detail meshes. For performance reasons (to save on draw calls) the lower you set this number the better.",
                "http://docs.unity3d.com/Documentation/Components/terrain-UsingTerrains.html");

            prefs.detailResolution = Mathf.Clamp(prefs.detailResolution, 32, 4096);

            prefs.resolutionPerPatch = IntPopup("Resolution Per Patch", prefs.resolutionPerPatch, labels2n4, values2n4,
                "Specifies the size in pixels of each individually rendered detail patch. A larger number reduces draw calls, but might increase triangle count since detail patches are culled on a per batch basis. A recommended value is 16. If you use a very large detail object distance and your grass is very sparse, it makes sense to increase the value.",
                "http://docs.unity3d.com/Documentation/ScriptReference/TerrainData.SetDetailResolution.html");

            if (prefs.detailResolution % prefs.resolutionPerPatch != 0)
                prefs.detailResolution = prefs.detailResolution / prefs.resolutionPerPatch * prefs.resolutionPerPatch;

            prefs.baseMapResolution = IntPopup("Base Map Resolution", prefs.baseMapResolution, labels2n, values2n,
                "The resolution of the composite texture that is used in place of the splat map at certain distances.",
                "http://docs.unity3d.com/Documentation/Components/terrain-UsingTerrains.html");
        }
        if (prefs.resultType == RealWorldTerrainResultType.terrain)
        {
            if (values2n1.All(v => v != prefs.heightmapResolution))
            {
                prefs.heightmapResolution = Mathf.ClosestPowerOfTwo(prefs.heightmapResolution) + 1;
                if (values2n1.All(v => v != prefs.heightmapResolution)) prefs.heightmapResolution = 129;
            }

            prefs.heightmapResolution =
                IntPopup("Height Map Resolution", prefs.heightmapResolution, labels2n1, values2n1,
                "The HeightMap resolution for each Terrain.", "http://docs.unity3d.com/Documentation/Components/terrain-UsingTerrains.html");
        }
        else if (prefs.resultType == RealWorldTerrainResultType.mesh)
        {
            prefs.heightmapResolution =
                RealWorldTerrainUtils.Limit(IntField("Height Map Resolution", prefs.heightmapResolution,
                "Total HeightMap resolution for all Meshes."), 32, 65536);
        }

#if T4M
        if (prefs.resultType == RealWorldTerrainResultType.T4M) OnT4MPerformance();
#endif

        GUILayout.Space(10);
    }

    private static void OnIdleTextureGUI()
    {
        prefs.textureProvider =
            (RealWorldTerrainTextureProvider)EditorGUILayout.EnumPopup("Provider: ", prefs.textureProvider);
        if (prefs.textureProvider == RealWorldTerrainTextureProvider.google)
            prefs.textureType = (RealWorldTerrainTextureType)EditorGUILayout.EnumPopup("Type: ", prefs.textureType);
        else if (prefs.textureProvider == RealWorldTerrainTextureProvider.custom)
        {
            prefs.textureProviderURL = EditorGUILayout.TextField("URL: ", prefs.textureProviderURL);
            showCustomProviderTokens = EditorGUILayout.Foldout(showCustomProviderTokens, "Available tokens");
            if (showCustomProviderTokens)
            {
                GUILayout.Label("{zoom}");
                GUILayout.Label("{x} - Tile X");
                GUILayout.Label("{y} - Tile Y");
                GUILayout.Label("{quad} - Quadkey");
                GUILayout.Space(10);
            }
        }

        if (prefs.resultType == RealWorldTerrainResultType.mesh)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Count textures.    X: ");
            prefs.textureCount.x = Mathf.Max(EditorGUILayout.IntField(prefs.textureCount.x), 1);
            GUILayout.Label("Y: ");
            prefs.textureCount.y = Mathf.Max(EditorGUILayout.IntField(prefs.textureCount.y), 1);
            GUILayout.EndHorizontal();
        }

        EditorGUILayout.BeginHorizontal();
        prefs.textureSize.x = EditorGUILayout.IntPopup("Texture width: ", prefs.textureSize.x, labels2n, values2n);
        prefs.textureSize.y = EditorGUILayout.IntPopup("height: ", prefs.textureSize.y, labels2n, values2n);
        EditorGUILayout.EndHorizontal();

        List<string> levels = new List<string> { "Auto" };
        for (int i = 5; i < 25; i++) levels.Add(i.ToString());
        int index = prefs.maxTextureLevel;
        if (index != 0) index -= 4;
        index = EditorGUILayout.Popup("Max level: ", index, levels.ToArray());
        prefs.maxTextureLevel = index;
        if (index != 0) prefs.maxTextureLevel += 4;
        else
        {
            prefs.reduceTextures = Toggle(prefs.reduceTextures, "Reduce size of textures, with no levels of tiles?", 
                "Reducing the size of the texture, reduces the time texture generation and memory usage.");
        }
        EditorGUILayout.Space();
    }

    private static bool Toggle(bool value, string text, string tooltip, string href = null)
    {
        EditorGUILayout.BeginHorizontal();

        value = GUILayout.Toggle(value, text);
        DrawHelpButton(tooltip, href);
        EditorGUILayout.EndHorizontal();

        return value;
    }

    private static void OnInsertCoords()
    {
        GUI.FocusControl("InsertCoordsButton");
        string nodeStr = EditorGUIUtility.systemCopyBuffer;
        if (nodeStr.IsNullOrEmpty()) return;

        XmlDocument doc = new XmlDocument();
        try
        {
            doc.LoadXml(nodeStr);
            XmlNode fnode = doc.FirstChild;
            if (fnode.Name == "Coords" && fnode.Attributes != null)
            {
                prefs.coordinatesFrom.x = fnode.Attributes["tlx"].GetFloat();
                prefs.coordinatesFrom.y = fnode.Attributes["tly"].GetFloat();
                prefs.coordinatesTo.x = fnode.Attributes["brx"].GetFloat();
                prefs.coordinatesTo.y = fnode.Attributes["bry"].GetFloat();

                XmlNodeList POInodes = fnode.SelectNodes("//POI");
                prefs.POI = new List<RealWorldTerrainPOI>();
                foreach (XmlNode node in POInodes) prefs.POI.Add(new RealWorldTerrainPOI(node));

                prefs.Save();
            }
        }
        catch { }
    }

    private static void OnProductPage()
    {
        Process.Start("http://infinity-code.com/products/real-world-terrain");
    }

    private static void OnReportError()
    {
        RealWorldTerrainReporter.OpenWindow();
    }

    private static void OnRunHelper()
    {
        string helperPath = "file://" +
                            Directory.GetFiles(Application.dataPath, "RWT_Helper.html", SearchOption.AllDirectories)[0]
                                .Replace('\\', '/');
        if (Application.platform == RuntimePlatform.OSXEditor) helperPath = helperPath.Replace(" ", "%20");
        prefs.Save();
        Application.OpenURL(helperPath);
    }

    private static void OnT4MPerformance()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("T4M Quality", GUILayout.Width(150));
        prefs.heightmapResolution = EditorGUILayout.IntField(prefs.heightmapResolution, GUILayout.Width(40));
        int verticlesCount = prefs.heightmapResolution * prefs.heightmapResolution;
        GUILayout.Label("x " + prefs.heightmapResolution + " : " + verticlesCount + " Verts");
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        prefs.heightmapResolution = (int)GUILayout.HorizontalScrollbar(prefs.heightmapResolution, 0, 32, 350);
        GUILayout.EndHorizontal();
        verticlesCount = prefs.heightmapResolution * prefs.heightmapResolution;
        showT4MPerformances = EditorGUILayout.Foldout(showT4MPerformances, "Vertex Performances (Approximate Indications)");

        if (showT4MPerformances)
        {
            const string T4MEditorFolder = "Assets/T4M/Editor/";
#if UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5_0
            Texture textureOK = Resources.LoadAssetAtPath(T4MEditorFolder + "Img/ok.png", typeof(Texture)) as Texture;
            Texture textureAvoid = Resources.LoadAssetAtPath(T4MEditorFolder + "Img/avoid.png", typeof(Texture)) as Texture;
            Texture textureKO = Resources.LoadAssetAtPath(T4MEditorFolder + "Img/ko.png", typeof(Texture)) as Texture;
#else
            Texture textureOK = AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Img/ok.png", typeof(Texture)) as Texture;
            Texture textureAvoid = AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Img/avoid.png", typeof(Texture)) as Texture;
            Texture textureKO = AssetDatabase.LoadAssetAtPath(T4MEditorFolder + "Img/ko.png", typeof(Texture)) as Texture;
#endif

            OnT4MPerformanceRow(verticlesCount, textureOK, textureAvoid, textureKO, "iPhone 3GS", 15000, 30000);
            OnT4MPerformanceRow(verticlesCount, textureOK, textureAvoid, textureKO, "iPad 1", 15000, 30000);
            OnT4MPerformanceRow(verticlesCount, textureOK, textureAvoid, textureKO, "iPhone 4", 20000, 40000);
            OnT4MPerformanceRow(verticlesCount, textureOK, textureAvoid, textureKO, "Tegra 2", 20000, 40000);
            OnT4MPerformanceRow(verticlesCount, textureOK, textureAvoid, textureKO, "iPad 2", 25000, 45000);
            OnT4MPerformanceRow(verticlesCount, textureOK, textureAvoid, textureKO, "iPhone 4S", 25000, 45000);
            OnT4MPerformanceRow(verticlesCount, textureOK, textureAvoid, textureKO, "Flash", 45000, 60000);
            OnT4MPerformanceRow(verticlesCount, textureOK, textureAvoid, textureKO, "Web", int.MaxValue, int.MaxValue);
            OnT4MPerformanceRow(verticlesCount, textureOK, textureAvoid, textureKO, "Desktop", int.MaxValue, int.MaxValue);
        }
    }

    private static void OnT4MPerformanceRow(int verticlesCount, Texture textureOK, Texture textureAvoid, Texture textureKO, string label, int minValue, int avoidValue)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(300));
        if (verticlesCount <= minValue) GUILayout.Label(textureOK);
        else if (verticlesCount > minValue && verticlesCount < avoidValue) GUILayout.Label(textureAvoid);
        else if (verticlesCount >= avoidValue) GUILayout.Label(textureKO);
        GUILayout.EndHorizontal();
    }

    private static void OnViewDocs()
    {
        Process.Start("http://infinity-code.com/docs/real-world-terrain");
    }

    private static void OnWebplayerWarning()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.wordWrap = true;
        GUILayout.Label("Real World Terrain can not work in a WebPlayer mode.\nSelect \"File / Build Settings\" to select another platform.", style);
        if (GUILayout.Button("Switch to Standalone"))
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTarget.StandaloneWindows);
        }
    }

    private static void OnSendMail()
    {
        Process.Start("mailto:support@infinity-code.com?subject=Real World Terrain");
    }
}