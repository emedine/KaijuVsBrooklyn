/*     INFINITY CODE 2013-2015      */
/*   http://www.infinity-code.com   */

#if !UNITY_4_3 && !UNITY_4_5 && !UNITY_4_6 && !UNITY_4_7 && !UNITY_5_0 && !UNITY_5_1 && !UNITY_5_2
#define UNITY_5_3P
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class RealWorldTerrain : EditorWindow
{
    public const string version = "2.6.0.19";

    public static RealWorldTerrainPhase _phase = RealWorldTerrainPhase.idle;
    public static RealWorldTerrainContainer container;
    public static RealWorldTerrainMonoBase generateTarget;
    public static RealWorldTerrainGenerateType generateType = RealWorldTerrainGenerateType.full;
    public static bool isCapturing;
    public static bool phaseComplete;
    public static int phaseIndex;
    public static float phaseProgress;
    public static float progress;
    public static RealWorldTerrainPrefs prefs;
    public static RealWorldTerrain wnd;

    private static RealWorldTerrainElevation activeElevation;
    private static RealWorldTerrainTexture activeTexture;
    private static bool loadComplete;
    private static bool partComplete;
    private static List<RealWorldTerrainPhase> requiredPhases;
    private static RealWorldTerrainItem[,] terrains;
    private static int textureLevel;
    private static Thread thread;

#if !UNITY_4_3 && !UNITY_4_5 && !UNITY_4_6 && !UNITY_4_7
    private static List<Light> lights;
#endif

    private static RealWorldTerrainPhase phase
    {
        get { return _phase; }
        set
        {
            _phase = value;
            progress = 0;
            switch (_phase)
            {
                case RealWorldTerrainPhase.downloading:
                {
                    RealWorldTerrainGUIUtils.phasetitle = "Downloading ";
                    break;
                }
                case RealWorldTerrainPhase.generateTerrains:
                {
                    RealWorldTerrainGUIUtils.phasetitle = "Generate terrains.";
                    break;
                }
                case RealWorldTerrainPhase.generateHeightmaps:
                {
                    RealWorldTerrainGUIUtils.phasetitle = "Generate heightmaps.";
                    break;
                }
                case RealWorldTerrainPhase.generateOSMBuildings:
                {
                    RealWorldTerrainGUIUtils.phasetitle = "Generate buildings.";
                    break;
                }
                case RealWorldTerrainPhase.generateOSMRoads:
                {
                    RealWorldTerrainGUIUtils.phasetitle = "Generate roads.";
                    break;
                }
                case RealWorldTerrainPhase.generateOSMGrass:
                {
                    RealWorldTerrainGUIUtils.phasetitle = "Generate grass.";
                    break;
                }
                case RealWorldTerrainPhase.generateOSMRivers:
                {
                    RealWorldTerrainGUIUtils.phasetitle = "Generate rivers.";
                    break;
                }
                case RealWorldTerrainPhase.generateOSMTrees:
                {
                    RealWorldTerrainGUIUtils.phasetitle = "Generate trees.";
                    break;
                }
                case RealWorldTerrainPhase.generateTextures:
                {
                    RealWorldTerrainGUIUtils.phasetitle = "Generate textures.";
                    break;
                }
                case RealWorldTerrainPhase.loadHeightmaps:
                {
                    RealWorldTerrainGUIUtils.phasetitle = "Loading elevation maps.";
                    break;
                }
                case RealWorldTerrainPhase.finish:
                {
                    RealWorldTerrainGUIUtils.phasetitle = "Finishing.";
                    break;
                }
                case RealWorldTerrainPhase.unzipHeightmaps:
                {
                    RealWorldTerrainGUIUtils.phasetitle = "Unzip elevation maps.";
                    break;
                }
            }
            wnd.Repaint();
        }
    }

    public static void CancelCapture()
    {
        Dispose();
    }

    private static bool CheckHeightmapMemory()
    {
        int count = prefs.terrainCount.magnitude;
        long size = prefs.heightmapResolution * prefs.heightmapResolution * 4 * count;
        size += prefs.baseMapResolution * prefs.baseMapResolution * 4 * count;
        size += prefs.detailResolution * prefs.detailResolution * 4 * count;
        size += 513 * 513 * 4 * count; //Alphamaps

        if (size > int.MaxValue * 0.75f)
        {
            return EditorUtility.DisplayDialog("Warning", "Too high settings. Perhaps out of memory error.", "Continue",
                "Abort");
        }
        return true;
    }

    public static void ClearCache()
    {
        RealWorldTerrainClearCache.OpenWindow();
    }

    private RealWorldTerrainItem CreateMesh(Transform parent, int x, int y, int ry, float x1, float y1, float x2,
        float y2, Vector3 size, Vector3 scale,
        float maxElevation, float minElevation)
    {
        GameObject GO = new GameObject(string.Format("Terrain {0}x{1}", x, ry));
        GO.transform.parent = parent;
        GO.transform.position = new Vector3(size.x * x, 0, size.z * y);

        return AddTerrainItem(x, y, x1, y1, x2, y2, size, scale, maxElevation, minElevation, GO);
    }

    private static RealWorldTerrainItem AddTerrainItem(int x, int y, float x1, float y1, float x2, float y2, Vector3 size,
        Vector3 scale, float maxElevation, float minElevation, GameObject GO)
    {
        RealWorldTerrainItem item = GO.AddComponent<RealWorldTerrainItem>();
        prefs.Apply(item);
        item.area = new Rect(x1, y1, x2 - x1, y2 - y1);
        item.maxElevation = maxElevation;
        item.minElevation = minElevation;
        item.scale = scale;
        item.size = size;
        item.x = x;
        item.y = y;

        return item;
    }

    private RealWorldTerrainItem CreateT4M(Transform parent, int x, int y, int ry, float x1, float y1, float x2,
        float y2, Vector3 size, Vector3 scale,
        float maxElevation, float minElevation)
    {
        GameObject GO = new GameObject(string.Format("Terrain {0}x{1}", x, ry));
        GO.transform.parent = parent;
        GO.transform.position = new Vector3(size.x * x, 0, size.z * y);

#if T4M
        GO.AddComponent<T4MObjSC>();
#endif
        return AddTerrainItem(x, y, x1, y1, x2, y2, size, scale, maxElevation, minElevation, GO);
    }

    private RealWorldTerrainItem CreateTerrain(Transform parent, int x, int y, int ry, float x1, float y1, float x2,
        float y2, Vector3 size, Vector3 scale,
        float maxElevation, float minElevation)
    {
        TerrainData tdata = new TerrainData
        {
            baseMapResolution = 32,
            heightmapResolution = 32
        };
        tdata.SetDetailResolution(prefs.detailResolution, prefs.resolutionPerPatch);
        tdata.size = size;
        GameObject GO = Terrain.CreateTerrainGameObject(tdata);

#if !UNITY_4_3 && !UNITY_4_5 && !UNITY_4_6 && !UNITY_4_7
        GO.isStatic = false;
#endif

        GO.name = string.Format("Terrain {0}x{1}", x, ry);
        GO.transform.parent = parent;
        GO.transform.position = new Vector3(size.x * x, 0, size.z * y);

        RealWorldTerrainItem item = GO.AddComponent<RealWorldTerrainItem>();
        prefs.Apply(item);
        item.area = new Rect(x1, y1, x2 - x1, y2 - y1);
        item.maxElevation = maxElevation;
        item.minElevation = minElevation;
        item.scale = scale;
        item.size = size;
        item.x = x;
        item.y = y;

        string filename = Path.Combine(container.folder, GO.name) + ".asset";
        AssetDatabase.CreateAsset(tdata, filename);
        AssetDatabase.SaveAssets();

        return item;
    }

#if TERRAVOL
    private RealWorldTerrainItem CreateTerraVol(Transform parent, int x, int y, int ry, float x1, float y1, float x2,
        float y2, Vector3 size, Vector3 scale,
        float maxElevation, float minElevation)
    {
        GameObject GO = new GameObject(string.Format("Terrain {0}x{1}", x, ry));
        GO.transform.parent = parent;
        GO.transform.position = new Vector3(size.x * x, 0, size.z * y);
        GO.AddComponent<TerraMapGenerator>();
        TerraMap map = GO.GetComponent<TerraMap>();
        BlockSet blockSet = GO.AddComponent<BlockSet>();
        blockSet.materials = new Material[1];
        blockSet.materials[0] = new Material(Shader.Find("Diffuse"));
        TerraVol.Block defaultBlock = new TerraVol.Block();
        defaultBlock.Name = "Default";

        blockSet.blocks = new List<TerraVol.Block> { defaultBlock };

        map.blockSet = blockSet;
        map.trees = new GameObject[0];
        map.treesDeepInTheGround = new float[0];
        map.blockSizeX = map.blockSizeY = map.blockSizeZ = 9;

        return AddTerrainItem(x, y, x1, y1, x2, y2, size, scale, maxElevation, minElevation, GO);
    }
#endif

    public static void Dispose()
    {
        isCapturing = false;

        if (thread != null)
        {
            thread.Abort();
            thread = null;
        }

        if (RealWorldTerrainElevation.elevations != null)
        {
            RealWorldTerrainElevation.elevations = null;
            RealWorldTerrainElevation.mesh = null;
            RealWorldTerrainElevation.meshTriangles = null;
            RealWorldTerrainElevation.meshUVs = null;
            RealWorldTerrainElevation.meshVerticles = null;
        }

        if (RealWorldTerrainTexture.textures != null)
        {
            foreach (RealWorldTerrainTexture texture in RealWorldTerrainTexture.textures) texture.Dispose();
            RealWorldTerrainTexture.textures = null;
        }

        RealWorldTerrainTexture.ready = false;
        RealWorldTerrainTexture.reqTextures = null;

        RealWorldTerrainDownloader.Dispose();
        RealWorldTerrainElevationBing.Dispose();
        RealWorldTerrainElevationSRTM.Dispose();
        RealWorldTerrainOSMRoad.Dispose();
        RealWorldTerrainOSMRivers.Dispose();
        RealWorldTerrainOSMGrass.Dispose();
        RealWorldTerrainOSMTree.Dispose();
        RealWorldTerrainOSMBuildingEditor.Dispose();

#if UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7
        EditorUtility.UnloadUnusedAssets();
#else
        EditorUtility.UnloadUnusedAssetsImmediate();

        if (lights != null)
        {
            foreach (Light light in lights) light.gameObject.SetActive(true);
            lights = null;
        }
#endif

        phaseProgress = 0;
        phaseIndex = 0;
        phaseComplete = false;

        GC.Collect();
    }

    private void Finish()
    {
#if RTP
        if (generateType == RealWorldTerrainGenerateType.full && prefs.resultType == RealWorldTerrainResultType.terrain && prefs.generateTextures)
        {
            ReliefTerrain reliefTerrain = terrains[0, 0].GetComponent<ReliefTerrain>();
            ReliefTerrainGlobalSettingsHolder settingsHolder = reliefTerrain.globalSettingsHolder;

            settingsHolder.numLayers = 4;
            settingsHolder.splats = new Texture2D[4];
            settingsHolder.Bumps = new Texture2D[4];
            settingsHolder.Heights = new Texture2D[4];

            for (int i = 0; i < 4; i++)
            {
                settingsHolder.splats[i] = RealWorldTerrainTexture.rtpTextures[i * 3];
                settingsHolder.Heights[i] = RealWorldTerrainTexture.rtpTextures[i * 3 + 1];
                settingsHolder.Bumps[i] = RealWorldTerrainTexture.rtpTextures[i * 3 + 2];
            }

            settingsHolder.GlobalColorMapBlendValues = new Vector3(1, 1, 1);
            settingsHolder._GlobalColorMapNearMIP = 1;
            settingsHolder.GlobalColorMapSaturation = 1;
            settingsHolder.GlobalColorMapSaturationFar = 1;
            settingsHolder.GlobalColorMapBrightness = 1;
            settingsHolder.GlobalColorMapBrightnessFar = 1;
            //settingsHolder.useTerrainMaterial = true;

            foreach (RealWorldTerrainItem item in terrains)
            {
#if UNITY_5_3P
                item.terrain.materialType = Terrain.MaterialType.Custom;
#endif
                item.GetComponent<ReliefTerrain>().RefreshTextures();
            }

            settingsHolder.Refresh();
        }
#endif
        if (generateType == RealWorldTerrainGenerateType.full && prefs.POI != null && prefs.POI.Count > 0)
        {
            GameObject poiContainer = new GameObject("POI");
            poiContainer.transform.parent = container.transform;

            foreach (RealWorldTerrainPOI poi in prefs.POI)
            {
                Vector3 pos = RealWorldTerrainEditorUtils.GlobalToLocalWithElevation(new Vector3(poi.x, 0, poi.y), container.scale);
                pos.y = RealWorldTerrainEditorUtils.NormalizeElevation(container, pos.y);
                GameObject poiGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
                poiGO.name = poi.title;
                poiGO.transform.parent = poiContainer.transform;
                poiGO.transform.position = pos;
#if UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7
                poiGO.renderer.sharedMaterial.color = Color.red;
#else
                poiGO.GetComponent<Renderer>().sharedMaterial.color = Color.red;
#endif
            }
        }

        if (prefs.resultType == RealWorldTerrainResultType.terrain)
        {
            if (terrains.Length > 1) GenerateNeighbors();

            foreach (RealWorldTerrainItem item in terrains)
            {
#if !UNITY_4_3 && !UNITY_4_5 && !UNITY_4_6 && !UNITY_4_7
                item.terrain.gameObject.isStatic = true;
#endif

                item.terrain.Flush();
            }
        }

        if (generateType == RealWorldTerrainGenerateType.texture && generateTarget is RealWorldTerrainItem)
            ((RealWorldTerrainItem) generateTarget).needUpdate = true;

        EditorGUIUtility.PingObject(container);

        isCapturing = false;
        phase = RealWorldTerrainPhase.idle;
        Dispose();
        Repaint();
    }

    private void GenerateBuildings()
    {
        try
        {
            if (prefs.generateBuildings)
            {
                if (prefs.buildingGenerator == 0) RealWorldTerrainOSMBuildingEditor.Generate(container);
                else if (prefs.buildingGenerator == 1) RealWorldTerrainOSMBuildREditor.Generate(container);

                progress = phaseProgress;
                Repaint();
            }
            if (!prefs.generateBuildings || phaseComplete)
            {
                NextPhase();
            }
        }
        catch (Exception exception)
        {
            Debug.Log(exception.Message);
            throw;
        }
    }

    private void GenerateGrass()
    {
        if (prefs.generateGrass)
        {
            RealWorldTerrainOSMGrass.Generate(container);
            progress = phaseProgress;
            Repaint();
        }
        if (!prefs.generateGrass || phaseComplete) NextPhase();
    }

    private void GenerateHeightmap(int index)
    {
        if (index >= terrains.Length)
        {
            RealWorldTerrainElevation.tdataHeightmap = null;
            NextPhase();
            return;
        }

        int x = index % prefs.terrainCount.x;
        int y = index / prefs.terrainCount.x;

        progress = (index + phaseProgress) / terrains.Length;
        Repaint();

        if (prefs.resultType == RealWorldTerrainResultType.terrain)
            RealWorldTerrainElevation.GenerateHeightMap(terrains[x, y]);
        else if (prefs.resultType == RealWorldTerrainResultType.mesh) 
            RealWorldTerrainElevation.GenerateMesh(terrains[x, y]);
#if T4M
        else if (prefs.resultType == RealWorldTerrainResultType.T4M)
            RealWorldTerrainElevation.GenerateT4M(terrains[x, y]);
#endif
#if TERRAVOL
        else if (prefs.resultType == RealWorldTerrainResultType.terraVol)
            RealWorldTerrainElevation.GenerateTerraVol(terrains[x, y]);
#endif

        if (phaseComplete)
        {
            phaseIndex++;
            phaseProgress = 0;
            phaseComplete = false;
        }
    }

    private void GenerateNeighbors()
    {
        for (int x = 0; x < prefs.terrainCount.x; x++)
        {
            for (int y = 0; y < prefs.terrainCount.y; y++)
            {
                Terrain bottom = (y > 0) ? terrains[x, y - 1].terrain : null;
                Terrain top = (y < prefs.terrainCount.y - 1) ? terrains[x, y + 1].terrain : null;
                Terrain left = (x > 0) ? terrains[x - 1, y].terrain : null;
                Terrain right = (x < prefs.terrainCount.x - 1) ? terrains[x + 1, y].terrain : null;
                terrains[x, y].terrain.SetNeighbors(left, top, right, bottom);
            }
        }
    }

    private void GenerateRivers()
    {
        if (prefs.generateRivers) RealWorldTerrainOSMRivers.Generate(container);
        NextPhase();
    }

    private void GenerateRoads()
    {
        if (prefs.generateRoads)
        {
            RealWorldTerrainOSMRoad.Generate(container);
            progress = phaseProgress;
            Repaint();
        }
        if (!prefs.generateRoads || phaseComplete) NextPhase();
    }

    private void GenerateTerrains()
    {
        progress = 0;

        if (generateTarget != null)
        {
            if (generateTarget is RealWorldTerrainContainer)
            {
                container = (RealWorldTerrainContainer) generateTarget;
                terrains = container.terrains;
            }
            else
            {
                RealWorldTerrainItem item = (RealWorldTerrainItem) generateTarget;
                container = item.container;
                terrains = new RealWorldTerrainItem[1, 1];
                terrains[0, 0] = item;
            }
        }

        if (generateType != RealWorldTerrainGenerateType.full)
        {
            NextPhase();
            return;
        }

        Repaint();

        string resultFolder = "Assets/RWT_Result";
        string resultFullPath = Path.Combine(Application.dataPath, "RWT_Result");
        if (!Directory.Exists(resultFullPath)) Directory.CreateDirectory(resultFullPath);
        string dateStr = DateTime.Now.ToString("yyyy-MM-dd HH-mm");
        resultFolder += "/" + dateStr;
        resultFullPath = Path.Combine(resultFullPath, dateStr);
        if (!Directory.Exists(resultFullPath)) Directory.CreateDirectory(resultFullPath);
        else
        {
            int index = 1;
            while (true)
            {
                string path = resultFullPath + "_" + index;
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    resultFolder += "_" + index;
                    break;
                }
                index++;
            }
        }

        const float R = 6371;
        const float scaleYCoof = 1112.0f;
        const float baseScale = 1000;

        Vector2 cFrom = prefs.coordinatesFrom;
        Vector2 cTo = prefs.coordinatesTo;
        Vector2 range = cFrom - cTo;
        RealWorldTerrainVector2i tCount = prefs.terrainCount;

        float sizeX = 0;
        float sizeY = 0;

        if (prefs.sizeType == 0)
        {
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
        else if (prefs.sizeType == 1)
        {
            const int eqv = 40075;
            sizeX = Mathf.Abs(range.x / 360 * eqv);
            sizeY = Mathf.Abs(range.y / 360 * eqv);
        }

        Vector3 size;
        float maxElevation = RealWorldTerrainUtils.MaxElevation;
        float minElevation = 0;

        if (prefs.maxElevationType == RealWorldTerrainMaxElevation.autoDetect)
        {
            float maxEl, minEl;
            RealWorldTerrainElevation.GetElevationRange(out minEl, out maxEl);
            maxElevation = maxEl;
            minElevation = minEl;
            size =
                Vector3.Scale(
                    new Vector3(sizeX / tCount.x, (maxEl - minEl) / scaleYCoof, sizeY / tCount.y) * baseScale,
                    prefs.terrainScale);
        }
        else
        {
            size =
                Vector3.Scale(
                    new Vector3(sizeX / tCount.x, RealWorldTerrainUtils.MaxElevation / scaleYCoof, sizeY / tCount.y) *
                    baseScale, prefs.terrainScale);
        }

        terrains = new RealWorldTerrainItem[tCount.x, tCount.y];
        Vector3 scale = new Vector3(size.x * tCount.x / -range.x, size.y / maxElevation, size.z * tCount.y / range.y);

        string baseName = Regex.Replace(RealWorldTerrainPrefs.LoadPref("ResultName", "RealWorld Terrain"), @"{\w+}", ReplaceToken);
        string containerName = baseName;

        if (RealWorldTerrainPrefs.LoadPref("AppendIndex", true))
        {
            int nameIndex = 0;

            while (GameObject.Find("/" + containerName))
            {
                nameIndex++;
                containerName = baseName + " " + nameIndex;
            }
        }

        container = new GameObject(containerName).AddComponent<RealWorldTerrainContainer>();
        prefs.Apply(container);
        container.area = new Rect(cFrom.x, cFrom.y, -range.x, -range.y);
        container.folder = resultFolder;
        container.scale = scale;
        container.size = new Vector3(size.x * tCount.x, size.y, size.z * tCount.y);
        container.terrainCount = prefs.terrainCount;
        container.minElevation = minElevation;
        container.maxElevation = maxElevation;

        for (int x = 0; x < tCount.x; x++)
        {
            for (int y = 0; y < tCount.y; y++)
            {
                float tx1 = Mathf.Lerp(cFrom.x, cTo.x, x / (float) tCount.x);
                float ty1 = Mathf.Lerp(cTo.y, cFrom.y, y / (float) tCount.y);
                float tx2 = Mathf.Lerp(cFrom.x, cTo.x, (x + 1) / (float) tCount.x);
                float ty2 = Mathf.Lerp(cTo.y, cFrom.y, (y + 1) / (float) tCount.y);
                int ry = tCount.y - y - 1;

                if (prefs.resultType == RealWorldTerrainResultType.terrain)
                {
                    terrains[x, y] = CreateTerrain(container.transform, x, y, ry, tx1, ty2, tx2, ty1, size, scale,
                        maxElevation, minElevation);
                }
                else if (prefs.resultType == RealWorldTerrainResultType.mesh)
                {
                    terrains[x, y] = CreateMesh(container.transform, x, y, ry, tx1, ty2, tx2, ty1, size, scale,
                        maxElevation, minElevation);
                }
#if T4M
                else if (prefs.resultType == RealWorldTerrainResultType.T4M)
                {
                    terrains[x, y] = CreateT4M(container.transform, x, y, ry, tx1, ty2, tx2, ty1, size, scale,
                        maxElevation, minElevation);
                }
#endif
#if TERRAVOL
                else if (prefs.resultType == RealWorldTerrainResultType.terraVol)
                {
                    terrains[x, y] = CreateTerraVol(container.transform, x, y, ry, tx1, ty2, tx2, ty1, size, scale,
                        maxElevation, minElevation);
                }
#endif
                terrains[x, y].container = container;
            }
        }

        container.terrains = terrains;

        NextPhase();
    }

    private void GenerateTextures(int index)
    {
        if (!prefs.generateTextures || index >= terrains.Length)
        {
            RealWorldTerrainTexture.colors = null;
            NextPhase();
            return;
        }

        int x = index % prefs.terrainCount.x;
        int y = index / prefs.terrainCount.x;

        progress = (index + phaseProgress) / terrains.Length;
        Repaint();

        RealWorldTerrainTexture.GenerateTexture(terrains[x, y]);

        if (phaseComplete)
        {
            phaseIndex++;
            phaseProgress = 0;
            phaseComplete = false;
        }
    }

    private void GenerateTrees()
    {
        if (prefs.generateTrees)
        {
            RealWorldTerrainOSMTree.Generate(container);
            progress = phaseProgress;
            Repaint();
        }
        if (!prefs.generateTrees || phaseComplete) NextPhase();
    }

    private static void InitPhases()
    {
        requiredPhases = new List<RealWorldTerrainPhase>();
        if (RealWorldTerrainDownloader.count > 0) requiredPhases.Add(RealWorldTerrainPhase.downloading);
        requiredPhases.Add(RealWorldTerrainPhase.unzipHeightmaps);
        requiredPhases.Add(RealWorldTerrainPhase.loadHeightmaps);
        requiredPhases.Add(RealWorldTerrainPhase.generateTerrains);
        if (generateType == RealWorldTerrainGenerateType.full || generateType == RealWorldTerrainGenerateType.terrain) requiredPhases.Add(RealWorldTerrainPhase.generateHeightmaps);
        if (generateType == RealWorldTerrainGenerateType.full || generateType == RealWorldTerrainGenerateType.texture) requiredPhases.Add(RealWorldTerrainPhase.generateTextures);
        if (generateType == RealWorldTerrainGenerateType.full || generateType == RealWorldTerrainGenerateType.additional)
        {
            if (prefs.generateBuildings) requiredPhases.Add(RealWorldTerrainPhase.generateOSMBuildings);
            if (prefs.generateRoads) requiredPhases.Add(RealWorldTerrainPhase.generateOSMRoads);
            if (prefs.generateGrass) requiredPhases.Add(RealWorldTerrainPhase.generateOSMGrass);
            if (prefs.generateTrees) requiredPhases.Add(RealWorldTerrainPhase.generateOSMTrees);
            if (prefs.generateRivers) requiredPhases.Add(RealWorldTerrainPhase.generateOSMRivers);
        }
        requiredPhases.Add(RealWorldTerrainPhase.finish);
        phase = RealWorldTerrainPhase.idle;

        NextPhase();
    }

    private static void LoadElevation()
    {
        try
        {
            if (prefs.elevationProvider == RealWorldTerrainElevationProvider.SRTM)
            {
                for (phaseIndex = 0; phaseIndex < RealWorldTerrainElevation.elevations.Count; phaseIndex++)
                {
                    activeElevation = RealWorldTerrainElevation.elevations[phaseIndex];
                    ((RealWorldTerrainElevationSRTM)activeElevation).ParseHeightmap();
                    if (!isCapturing) return;
                }
            }
            else if (prefs.elevationProvider == RealWorldTerrainElevationProvider.BingMaps)
            {
                if (!RealWorldTerrainElevationBing.Load())
                {
                    isCapturing = false;
                    Dispose();
                    Debug.LogError("Cannot load elevation map");
                    return;
                }
            }

            loadComplete = true;
        }
        catch (Exception exception)
        {
            Debug.Log(exception.Message + "\n" + exception.StackTrace);
            throw;
        }
        
    }

    private static void NextPhase()
    {
        if (_phase == RealWorldTerrainPhase.idle) phase = requiredPhases[0];
        else phase = requiredPhases[requiredPhases.IndexOf(_phase) + 1];

        phaseComplete = false;
        phaseIndex = 0;
        phaseProgress = 0;
        wnd.Repaint();

        if (_phase == RealWorldTerrainPhase.downloading) RealWorldTerrainDownloader.Start();
        else if (_phase == RealWorldTerrainPhase.unzipHeightmaps) UnzipHeightmap(0);
    }

// ReSharper disable once UnusedMember.Local
    private void OnDestroy()
    {
        wnd = null;
        prefs.Save();
    }

// ReSharper disable once UnusedMember.Local
    private void OnEnable()
    {
        wnd = this;
        if (prefs == null) prefs = new RealWorldTerrainPrefs();
        prefs.Load();
        
        RealWorldTerrainUpdater.CheckNewVersionAvailable();
    }

// ReSharper disable once UnusedMember.Local
    private void OnGUI()
    {
        RealWorldTerrainGUIUtils.OnGUI();
    }

    [MenuItem("Window/Infinity Code/Real World Terrain/Open Real World Terrain", false, 0)]
// ReSharper disable once UnusedMember.Local
    private static void OpenWindow()
    {
        OpenWindow(RealWorldTerrainGenerateType.full, null);
    }

    public static void OpenWindow(RealWorldTerrainGenerateType type, RealWorldTerrainMonoBase target)
    {
        generateTarget = target;
        generateType = type;
        wnd = GetWindow<RealWorldTerrain>(false, "Real World Terrain");
        if (target == null)
        {
            prefs = new RealWorldTerrainPrefs();
            prefs.Load();
        }
        else if (type == RealWorldTerrainGenerateType.full)
        {
            prefs = RealWorldTerrainPrefs.GetPrefs(target, true);
            generateTarget = null;
        }
        else prefs = RealWorldTerrainPrefs.GetPrefs(target);
        RealWorldTerrainUpdater.CheckNewVersionAvailable();
        DontDestroyOnLoad(wnd);
    }

    private static void PrepareStart()
    {
        if (prefs.coordinatesFrom.x >= prefs.coordinatesTo.x)
        {
            Debug.Log("Bottom-Right Longitude must be greater than Top-Left Longitude");
            return;
        }
        if (prefs.coordinatesFrom.y <= prefs.coordinatesTo.y)
        {
            Debug.Log("Top-Left Latitude must be greater than Bottom-Right Latitude");
            return;
        }
        if (prefs.coordinatesFrom.x < -180 || prefs.coordinatesTo.x < -180 || prefs.coordinatesFrom.x > 180 ||
            prefs.coordinatesTo.x > 180)
        {
            Debug.Log("Longitude must be between -180 and 180.");
            return;
        }

        Vector2 cStart = RealWorldTerrainUtils.LanLongToFlat(prefs.coordinatesFrom);
        Vector2 cEnd = RealWorldTerrainUtils.LanLongToFlat(prefs.coordinatesTo);

        if ((cStart.y < 30 || cEnd.y > 150) && prefs.elevationProvider == RealWorldTerrainElevationProvider.SRTM)
        {
            Debug.Log("Latitude in empty range.");
            return;
        }

        if (prefs.elevationProvider == RealWorldTerrainElevationProvider.BingMaps)
        {
            RealWorldTerrainElevationBing.key = RealWorldTerrainPrefs.LoadPref("BingAPI", string.Empty);
            if (string.IsNullOrEmpty(RealWorldTerrainElevationBing.key))
            {
                Debug.LogError("Bing Maps API key is not specified.");
                return;
            }
        }

        if (prefs.resultType == RealWorldTerrainResultType.terrain && !CheckHeightmapMemory()) return;

        Vector3 range = prefs.coordinatesTo - prefs.coordinatesFrom;
        range.x = Mathf.Abs(range.x);
        range.y = Mathf.Abs(range.y);

        if (prefs.resultType == RealWorldTerrainResultType.mesh)
        {
            prefs.terrainCount = prefs.textureCount;
        }
        else if (prefs.resultType == RealWorldTerrainResultType.terrain)
        {
            prefs.textureCount = prefs.terrainCount;
        }
#if T4M
        else if (prefs.resultType == RealWorldTerrainResultType.T4M)
        {
            prefs.terrainCount = RealWorldTerrainVector2i.one;
            prefs.textureCount = RealWorldTerrainVector2i.one;
        }
#endif

        RealWorldTerrainElevation.elevations = new List<RealWorldTerrainElevation>();

        if (generateType == RealWorldTerrainGenerateType.full || generateType == RealWorldTerrainGenerateType.terrain ||
            generateType == RealWorldTerrainGenerateType.additional)
        {
            if (prefs.elevationProvider == RealWorldTerrainElevationProvider.SRTM) RealWorldTerrainElevationSRTM.Init(cStart, cEnd, range);
            else if (prefs.elevationProvider == RealWorldTerrainElevationProvider.BingMaps) RealWorldTerrainElevationBing.Init();
        }

        if (generateType == RealWorldTerrainGenerateType.full || generateType == RealWorldTerrainGenerateType.texture)
        {
            if (prefs.generateTextures)
            {
                if (prefs.maxTextureLevel == 0)
                {
                    float dpa =
                        Mathf.Max(prefs.textureSize.x * prefs.terrainCount.x, prefs.textureSize.y * prefs.terrainCount.y) /
                        Mathf.Max(range.x, range.y);
                    textureLevel = 0;

                    for (int i = 5; i < 24; i++)
                    {
                        float tiles = Mathf.Pow(2, i);
                        float cdpa = 256 * tiles / 360;
                        if (cdpa > dpa)
                        {
                            textureLevel = i;
                            break;
                        }
                    }

                    if (textureLevel == 0) textureLevel = 24;
                }
                else textureLevel = prefs.maxTextureLevel;

                if (!RealWorldTerrainTexture.Init(textureLevel)) return;
            }
        }

        if (generateType == RealWorldTerrainGenerateType.full || generateType == RealWorldTerrainGenerateType.additional)
        {
            RealWorldTerrainOSMBuildingEditor.Download();
            RealWorldTerrainOSMRivers.Download();
            RealWorldTerrainOSMRoad.Download();
            RealWorldTerrainOSMTree.Download();
            RealWorldTerrainOSMGrass.Download();
        }

#if !UNITY_4_3 && !UNITY_4_5 && !UNITY_4_6 && !UNITY_4_7
        lights = FindObjectsOfType<Light>().ToList();
        lights.RemoveAll(l => !l.gameObject.activeSelf);
        foreach (Light light in lights) light.gameObject.SetActive(false);;
#endif

        loadComplete = false;
        isCapturing = true;

        InitPhases();
    }

    private string ReplaceToken(Match match)
    {
        string v = match.Value.ToLower().Trim('{', '}');
        if (v == "tllat") return prefs.coordinatesFrom.y.ToString();
        if (v == "tllng") return prefs.coordinatesFrom.x.ToString();
        if (v == "brlat") return prefs.coordinatesTo.y.ToString();
        if (v == "brlng") return prefs.coordinatesTo.x.ToString();
        if (v == "cx") return prefs.terrainCount.x.ToString();
        if (v == "cy") return prefs.terrainCount.y.ToString();
        if (v == "st") return prefs.sizeType.ToString();
        if (v == "me") return prefs.maxElevationType.ToString();
        if (v == "mu") return prefs.nodataValue.ToString();
        if (v == "ds") return prefs.depthSharpness.ToString();
        if (v == "dr") return prefs.detailResolution.ToString();
        if (v == "rpp") return prefs.resolutionPerPatch.ToString();
        if (v == "bmr") return prefs.baseMapResolution.ToString();
        if (v == "hmr") return prefs.heightmapResolution.ToString();
        if (v == "tp") return prefs.textureProvider.ToString();
        if (v == "tw") return prefs.textureSize.x.ToString();
        if (v == "th") return prefs.textureSize.y.ToString();
        if (v == "tml") return prefs.maxTextureLevel.ToString();
        if (v == "ticks") return DateTime.Now.Ticks.ToString();
        return v;
    }

    public static void StartCapture()
    {
        prefs.Save();

        if (generateType == RealWorldTerrainGenerateType.additional)
        {
            RealWorldTerrainMonoBase target = generateTarget;

            if (target is RealWorldTerrainContainer)
            {
                if (prefs.generateBuildings) RealWorldTerrainUtils.DeleteGameObject(target.transform, "Buildings");
                if (prefs.generateRivers) RealWorldTerrainUtils.DeleteGameObject(target.transform, "Rivers");
                if (prefs.generateRoads) RealWorldTerrainUtils.DeleteGameObject(target.transform, "Roads");

                foreach (RealWorldTerrainItem item in (target as RealWorldTerrainContainer).terrains)
                {
                    item.terrainData.treeInstances = new TreeInstance[0];
                    item.terrainData.treePrototypes = new TreePrototype[0];
                    item.terrainData.detailPrototypes = new DetailPrototype[0];
                }
            }
            else
            {
                RealWorldTerrainItem item = target as RealWorldTerrainItem;

                string index = item.x + "x" + (item.container.terrainCount.y - item.y - 1);
                if (prefs.generateBuildings) RealWorldTerrainUtils.DeleteGameObject(target.transform.parent, "Buildings " + index);
                if (prefs.generateRivers) RealWorldTerrainUtils.DeleteGameObject(target.transform.parent, "Rivers " + index);
                if (prefs.generateRoads) RealWorldTerrainUtils.DeleteGameObject(target.transform.parent, "Roads " + index);

                item.terrainData.treeInstances = new TreeInstance[0];
                item.terrainData.treePrototypes = new TreePrototype[0];
                item.terrainData.detailPrototypes = new DetailPrototype[0];
            }
        }

        PrepareStart();
    }

    private static void UnzipHeightmap(int index)
    {
        if (prefs.elevationProvider != RealWorldTerrainElevationProvider.SRTM || index >= RealWorldTerrainElevation.elevations.Count)
        {
            thread = new Thread(LoadElevation);
            thread.Start();
            NextPhase();
            return;
        }

        phaseIndex = index;
        activeElevation = RealWorldTerrainElevation.elevations[phaseIndex];

        progress = phaseIndex / (float) RealWorldTerrainElevation.elevations.Count;
        wnd.Repaint();

        new Thread(((RealWorldTerrainElevationSRTM)activeElevation).UnzipHeightmap).Start();
    }

// ReSharper disable once UnusedMember.Local
    private void Update()
    {
        if (!isCapturing) return;

        if (phase == RealWorldTerrainPhase.downloading)
        {
            if (!RealWorldTerrainDownloader.finish)
            {
                RealWorldTerrainDownloader.CheckComplete();
                progress = RealWorldTerrainDownloader.progress;
                Repaint();
            }
            else NextPhase();
        }
        else if (phase == RealWorldTerrainPhase.unzipHeightmaps)
        {
            if (((RealWorldTerrainElevationSRTM)activeElevation).unziped) UnzipHeightmap(phaseIndex + 1);
        }
        else if (phase == RealWorldTerrainPhase.loadHeightmaps)
        {
            if (loadComplete)
            {
                NextPhase();
                return;
            }
            if (RealWorldTerrainElevation.elevations != null)
            {
                progress = (phaseIndex + phaseProgress) / RealWorldTerrainElevation.elevations.Count;
                int cProgress = Mathf.FloorToInt(progress * 100);
                if (cProgress != RealWorldTerrainGUIUtils.iProgress)
                {
                    RealWorldTerrainGUIUtils.iProgress = cProgress;
                    Repaint();
                }
            }
        }
        else if (phase == RealWorldTerrainPhase.generateTerrains) GenerateTerrains();
        else if (phase == RealWorldTerrainPhase.generateHeightmaps) GenerateHeightmap(phaseIndex);
        else if (phase == RealWorldTerrainPhase.generateTextures) GenerateTextures(phaseIndex);
        else if (phase == RealWorldTerrainPhase.generateOSMBuildings) GenerateBuildings();
        else if (phase == RealWorldTerrainPhase.generateOSMRoads) GenerateRoads();
        else if (phase == RealWorldTerrainPhase.generateOSMGrass) GenerateGrass();
        else if (phase == RealWorldTerrainPhase.generateOSMTrees) GenerateTrees();
        else if (phase == RealWorldTerrainPhase.generateOSMRivers) GenerateRivers();
        else if (phase == RealWorldTerrainPhase.finish) Finish();
    }
}