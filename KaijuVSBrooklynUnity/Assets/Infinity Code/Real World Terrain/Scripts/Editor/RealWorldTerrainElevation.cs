/*     INFINITY CODE 2013-2015      */
/*   http://www.infinity-code.com   */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using InfinityCode.Zip;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class RealWorldTerrainElevation
{
    public static List<RealWorldTerrainElevation> elevations;
    public static Mesh mesh;
    public static int[] meshTriangles;
    public static Vector2[] meshUVs;
    public static Vector3[] meshVerticles;
    public static float[,] tdataHeightmap;

    protected static float depthStep;
    protected static RealWorldTerrainElevation lastElevation;
    protected static int lastX;
    protected static int lastY;
    protected static int mapSize;
    protected static bool meshItemCreated;
    protected static bool multiMeshCreated;

    private static float curDepth;
    private static bool hasUnderwater;
    private static float nextZ;
    private static TerrainData tdata;

    public short[,] heightmap;

    protected static RealWorldTerrainPrefs prefs
    {
        get { return RealWorldTerrain.prefs; }
    }

    public virtual bool Contains(float X, float Y)
    {
        return false;
    }

    private bool Contains(Vector2 point)
    {
        return Contains(point.x, point.y);
    }

    public static void GenerateMesh(RealWorldTerrainItem item)
    {
        if (GenerateMeshItem(item)) return;

        int hmsx = Mathf.RoundToInt(item.heightmapResolution / (float)item.textureCount.x + 0.49f);
        int hmsy = Mathf.RoundToInt(item.heightmapResolution / (float)item.textureCount.y + 0.49f);

        int count = Mathf.RoundToInt(hmsx * hmsy / 65000f + 0.49999f);

        if (count > 1)
        {
            if (GenerateMultiMesh(item, hmsx, hmsy)) return;
        }
        else GenerateSingleMesh(item);

        lastX = 0;
        lastY = 0;
        meshItemCreated = false;

        RealWorldTerrain.phaseComplete = true;
    }

    private static bool GenerateMultiMesh(RealWorldTerrainItem item, int hmsx, int hmsy)
    {
        if (GenerateMultiMeshItem(item, hmsx, hmsy)) return true;

        int index = lastY;

        long startTime = DateTime.Now.Ticks;
        
        for (int i = lastX; i < index; i++)
        {
            string id = item.name + "x" + i;
            string filename = Path.Combine(item.container.folder, id + ".asset");
            Vector3 position = new Vector3(0, 0, nextZ);

            GameObject GO = new GameObject(id);
            GO.transform.parent = item.transform;
            GO.transform.localPosition = position;

            mesh = AssetDatabase.LoadAssetAtPath(filename, typeof (Mesh)) as Mesh;
            GO.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshCollider cl = GO.AddComponent<MeshCollider>();
            cl.sharedMesh = mesh;
            GO.AddComponent<MeshRenderer>();

            nextZ = cl.bounds.max.z - item.transform.position.z;

            if (new TimeSpan(DateTime.Now.Ticks - startTime).TotalSeconds > 1)
            {
                lastX = i + 1;
                RealWorldTerrain.phaseProgress = 0.2f + 0.8f * i / index;
                return true;
            }
        }

        return false;
    }

    private static bool GenerateMultiMeshItem(RealWorldTerrainItem item, int hmsx, int hmsy)
    {
        if (multiMeshCreated) return false;

        int rowPerMesh = 65000 / hmsx;

        int totalVerticles = 0;
        Vector3[] subMeshVerticles = new Vector3[0];
        Vector2[] subMeshUVs = new Vector2[0];
        int[] subMeshTriangles = new int[0];

        int startRow = lastX;
        int index = lastY;

        long startTime = DateTime.Now.Ticks;

        while (true)
        {
            int rowCount = (startRow + rowPerMesh <= hmsy) ? rowPerMesh : hmsy - startRow;

            if (totalVerticles != hmsx * rowCount)
            {
                totalVerticles = hmsx * rowCount;
                subMeshVerticles = new Vector3[totalVerticles];
                subMeshUVs = new Vector2[totalVerticles];
                subMeshTriangles = new int[totalVerticles * 6];
            }

            Vector3 position = new Vector3(0, 0, startRow / (float) (hmsy - 1) * item.size.z);

            for (int x = 0; x < hmsx; x++)
            {
                for (int y = 0; y < rowCount; y++)
                {
                    int v = ((y + startRow) * hmsx) + x;
                    int sv = (y * hmsx) + x;
                    subMeshVerticles[sv] = meshVerticles[v] - position;
                    subMeshUVs[sv] = meshUVs[v];

                    if (x < hmsx - 1 && y < rowCount - 1)
                    {
                        int mv = sv * 6;
                        subMeshTriangles[mv] = sv;
                        subMeshTriangles[mv + 1] = sv + hmsx;
                        subMeshTriangles[mv + 2] = sv + 1;
                        subMeshTriangles[mv + 3] = sv + 1;
                        subMeshTriangles[mv + 4] = sv + hmsx;
                        subMeshTriangles[mv + 5] = sv + hmsx + 1;
                    }
                }
            }

            mesh = new Mesh {vertices = subMeshVerticles, triangles = subMeshTriangles, uv = subMeshUVs};
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            string id = item.name + "x" + index;
            string filename = Path.Combine(item.container.folder, id + ".asset");

            AssetDatabase.CreateAsset(mesh, filename);
            AssetDatabase.SaveAssets();

            startRow += rowPerMesh - 1;
            index++;
            if (startRow >= hmsy) break;

            if (new TimeSpan(DateTime.Now.Ticks - startTime).TotalSeconds > 1)
            {
                lastX = startRow;
                lastY = index;
                RealWorldTerrain.phaseProgress = 0.1f + 0.1f * startRow / hmsy;
                return true;
            }
        }

        lastY = index;
        lastX = 0;
        nextZ = 0;
        multiMeshCreated = true;

        AssetDatabase.Refresh();
        return false;
    }

    private static void GenerateSingleMesh(RealWorldTerrainItem item)
    {
        mesh = new Mesh();
        mesh.vertices = meshVerticles;
        mesh.triangles = meshTriangles;
        mesh.uv = meshUVs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        string filename = Path.Combine(item.container.folder, item.name + ".asset");
        AssetDatabase.CreateAsset(mesh, filename);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        mesh = AssetDatabase.LoadAssetAtPath(filename, typeof (Mesh)) as Mesh;
        item.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        item.gameObject.AddComponent<MeshCollider>().sharedMesh = mesh;
        item.gameObject.AddComponent<MeshRenderer>();
    }

    private static bool GenerateMeshItem(RealWorldTerrainItem item)
    {
        if (meshItemCreated) return false;
        int hrX = Mathf.RoundToInt(item.heightmapResolution / (float) item.textureCount.x);
        int hrY = Mathf.RoundToInt(item.heightmapResolution / (float) item.textureCount.y);

        int thiX = hrX - 1;
        int thiY = hrY - 1;

        if (mesh == null)
        {
            mesh = new Mesh();
            int verticlesCount = hrX * hrY;
            meshVerticles = new Vector3[verticlesCount];
            meshTriangles = new int[thiX * thiY * 6];
            meshUVs = new Vector2[verticlesCount];
            lastY = 0;
        }

        float x1 = item.area.x;
        float x2 = item.area.x + item.area.width;
        float y1 = item.area.y;
        float y2 = item.area.y + item.area.height;
        float minElevation = item.minElevation;
        float elevationRange = item.maxElevation - minElevation;

        long startTime = DateTime.Now.Ticks;

        float thfX = thiX;
        float thfY = thiY;
        Vector3 s = item.size;
        s.x /= thfX;
        s.z /= thfY;

        float scaledRange = elevationRange / s.y;
        float nodataDepth = (prefs.nodataValue - minElevation) / scaledRange;

        for (int hy = lastY; hy < hrY; hy++)
        {
            float ry = hy / thfY;
            float py = Mathf.Lerp(y2, y1, ry);
            int iy = hy * hrX;
            float verticleY = hy * s.z;

            for (int hx = 0; hx < hrX; hx++)
            {
                float rx = hx / thfX;
                float px = Mathf.Lerp(x1, x2, rx);

                float elevation = GetElevation(px, py);
                int v = iy + hx;
                float cy = float.MinValue;
                if (Math.Abs(elevation - float.MinValue) > 0.1f) cy = (elevation - minElevation) / scaledRange;
                else if (prefs.nodataValue == 0) cy = nodataDepth;
                else hasUnderwater = true;

                meshVerticles[v] = new Vector3(hx * s.x, cy, verticleY);
                meshUVs[v] = new Vector2(rx, ry);

                if (hx < thiX && hy < thiY)
                {
                    int mv = (hy * thiX + hx) * 6;
                    meshTriangles[mv] = v;
                    meshTriangles[mv + 1] = v + hrX;
                    meshTriangles[mv + 2] = v + 1;
                    meshTriangles[mv + 3] = v + 1;
                    meshTriangles[mv + 4] = v + hrX;
                    meshTriangles[mv + 5] = v + hrX + 1;
                }
            }

            lastY = hy + 1;
            RealWorldTerrain.phaseProgress = 0.1f * hy / hrY;
            if (new TimeSpan(DateTime.Now.Ticks - startTime).TotalSeconds > 1) return true;
        }

        while (hasUnderwater)
        {
            bool newHasUnderwater = false;
            bool fillMaxDepth = false;
            float prevDepth = (curDepth - minElevation) / elevationRange;
            curDepth -= depthStep;
            if (curDepth <= prefs.nodataValue)
            {
                curDepth = prefs.nodataValue;
                fillMaxDepth = true;
            }

            float cDepth = (curDepth - minElevation) / elevationRange * s.y;

            for (int hy = 0; hy < hrY; hy++)
            {
                bool ignoreTop = false;
                int cy = hy * hrX;
                for (int hx = 0; hx < hrX; hx++)
                {
                    int cx = cy + hx;
                    if (meshVerticles[cx].y == float.MinValue)
                    {
                        bool ignoreLeft = hx > 0 && meshVerticles[cx - 1].y != prevDepth;
                        if (fillMaxDepth || IsSingleDistance(hx, hy, ignoreLeft, ignoreTop))
                        {
                            meshVerticles[cx].y = cDepth;
                            ignoreTop = true;
                        }
                        else
                        {
                            newHasUnderwater = true;
                            ignoreTop = false;
                        }
                    }
                    else ignoreTop = false;
                }
            }

            hasUnderwater = newHasUnderwater;
            if (new TimeSpan(DateTime.Now.Ticks - startTime).TotalSeconds > 1) return true;
        }

        lastY = 0;
        curDepth = 0;
        meshItemCreated = true;

        return false;
    }

    public static void GenerateHeightMap(RealWorldTerrainItem item)
    {
        if (tdata == null) InitTData(item);

        float x1 = item.area.x;
        float x2 = item.area.x + item.area.width;
        float y1 = item.area.y;
        float y2 = item.area.y + item.area.height;
        float minElevation = item.minElevation;
        float elevationRange = item.maxElevation - minElevation;

        long startTime = DateTime.Now.Ticks;

        float thx = tdata.heightmapWidth - 1;
        float thy = tdata.heightmapHeight - 1;

        for (int hx = lastX; hx < tdata.heightmapWidth; hx++)
        {
            float px = Mathf.Lerp(x1, x2, hx / thx);
            for (int hy = 0; hy < tdata.heightmapHeight; hy++)
            {
                float py = Mathf.Lerp(y2, y1, hy / thy);
                float elevation = GetElevation(px, py);
                if (Math.Abs(elevation - float.MinValue) > 0.1f)
                {
                    tdataHeightmap[hy, hx] = (elevation - minElevation) / elevationRange;
                }
                else if (prefs.nodataValue == 0)
                    tdataHeightmap[hy, hx] = (prefs.nodataValue - minElevation) / elevationRange;
                else
                {
                    hasUnderwater = true;
                    tdataHeightmap[hy, hx] = float.MinValue;
                }
            }
            lastX = hx + 1;
            RealWorldTerrain.phaseProgress = hx / (float)tdata.heightmapWidth;
            if (new TimeSpan(DateTime.Now.Ticks - startTime).TotalSeconds > 1) return;
        }

        while (hasUnderwater)
        {
            bool newHasUnderwater = false;
            bool fillMaxDepth = false;
            float prevDepth = (curDepth - minElevation) / elevationRange;
            curDepth -= depthStep;
            if (curDepth <= prefs.nodataValue)
            {
                curDepth = prefs.nodataValue;
                fillMaxDepth = true;
            }

            for (int hx = 0; hx < tdata.heightmapWidth; hx++)
            {
                bool ignoreTop = false;
                for (int hy = 0; hy < tdata.heightmapHeight; hy++)
                {
                    if (tdataHeightmap[hy, hx] == float.MinValue)
                    {
                        bool ignoreLeft = hx > 0 && tdataHeightmap[hy, hx - 1] != prevDepth;
                        if (fillMaxDepth || IsSingleDistance(hx, hy, ignoreLeft, ignoreTop))
                        {
                            tdataHeightmap[hy, hx] = (curDepth - minElevation) / elevationRange;
                            ignoreTop = true;
                        }
                        else
                        {
                            newHasUnderwater = true;
                            ignoreTop = false;
                        }
                    }
                    else ignoreTop = false;
                }
            }

            hasUnderwater = newHasUnderwater;
            if (new TimeSpan(DateTime.Now.Ticks - startTime).TotalSeconds > 1) return;
        }

        lastX = 0;
        curDepth = 0;
        tdata.SetHeights(0, 0, tdataHeightmap);
        tdata = null;

        RealWorldTerrain.phaseComplete = true;
    }

#if T4M
    public static void GenerateT4M(RealWorldTerrainItem item)
    {
        if (GenerateMeshItem(item)) return;

        string filename = Path.Combine(item.container.folder, item.name) + ".obj";

        StreamWriter sw = new StreamWriter(filename);
        try
        {
            sw.WriteLine("# T4M File");
            for (int i = 0; i < meshVerticles.Length; i++)
            {
                StringBuilder sb = new StringBuilder("v ", 20);
                sb.Append("-" + meshVerticles[i].x).Append(" ")
                    .Append(meshVerticles[i].y.ToString()).Append(" ")
                    .Append(meshVerticles[i].z.ToString());
                sw.WriteLine(sb);
            }

            for (int i = 0; i < meshUVs.Length; i++)
            {
                StringBuilder sb = new StringBuilder("vt ", 22);
                sb.Append(meshUVs[i].x.ToString()).Append(" ").Append(meshUVs[i].y.ToString());
                sw.WriteLine(sb);
            }
            for (int i = 0; i < meshTriangles.Length; i += 3)
            {
                StringBuilder sb = new StringBuilder("f ", 43);
                sb.Append(meshTriangles[i] + 1).Append("/").Append(meshTriangles[i] + 1).Append(" ").
                    Append(meshTriangles[i + 2] + 1).Append("/").Append(meshTriangles[i + 2] + 1).Append(" ").
                    Append(meshTriangles[i + 1] + 1).Append("/").Append(meshTriangles[i + 1] + 1);
                sw.WriteLine(sb);
            }
        }
        catch {}
        sw.Close();

        meshTriangles = null;
        meshUVs = null;
        meshVerticles = null;

        AssetDatabase.Refresh();

        ModelImporter modelImporter = ModelImporter.GetAtPath(filename) as ModelImporter;
        if (modelImporter != null)
        {
            modelImporter.globalScale = 1;
            modelImporter.splitTangentsAcrossSeams = true;
            modelImporter.normalImportMode = ModelImporterTangentSpaceMode.Calculate;
            modelImporter.tangentImportMode = ModelImporterTangentSpaceMode.Calculate;
            modelImporter.generateAnimations = ModelImporterGenerateAnimations.None;
            modelImporter.meshCompression = ModelImporterMeshCompression.Off;
            modelImporter.normalSmoothingAngle = 180f;
            AssetDatabase.ImportAsset(filename, ImportAssetOptions.ForceSynchronousImport);
        }

        GameObject GO = AssetDatabase.LoadAssetAtPath(filename, typeof (GameObject)) as GameObject;
        if (GO != null)
        {
            GO = Object.Instantiate(GO) as GameObject;

            T4MObjSC obj = item.GetComponent<T4MObjSC>();
            MeshFilter[] mfs = GO.GetComponentsInChildren<MeshFilter>();
            obj.T4MMesh = mfs[0];

            foreach (MeshFilter mf in mfs)
            {
                GameObject subMesh = mf.gameObject;
                subMesh.transform.parent = item.transform;
                subMesh.layer = 30;
                subMesh.isStatic = true;
                subMesh.AddComponent<T4MPartSC>();
                subMesh.AddComponent<MeshCollider>();
            }
            Object.DestroyImmediate(GO);
        }

        mesh = null;

        RealWorldTerrain.phaseComplete = true;
    }
#endif

#if TERRAVOL
    public static void GenerateTerraVol(RealWorldTerrainItem item)
    {
        TerraMap map = item.GetComponent<TerraMap>();
        TerraMapGenerator generator = item.GetComponent<TerraMapGenerator>();
        map.Awake();
        generator.Awake();

        Debug.Log(item.size);

        int sx = Mathf.RoundToInt(item.size.x / 8 / map.blockSizeX);
        int sz = Mathf.RoundToInt(item.size.z / 8 / map.blockSizeZ);

        map.blockSet.materials[0].mainTextureScale = new Vector2(1f / sx / 8 / map.blockSizeX, 1f / sz / 8 / map.blockSizeZ);

        for (int cx = -1; cx < sx + 1; cx++)
        {
            for (int cz = -1; cz < sz + 1; cz++)
            {
                TerraVol.Vector2i p = new TerraVol.Vector2i(cx, cz);
                TerraVol.Chunk2D chunk2D = map.TerraMap2DGetCreate(p);
                generator.InitColumn(cx, cz);
                GenerateChunk2D(map, chunk2D, item, cx * Chunk.SIZE_X + 2, cz * Chunk.SIZE_Z + 2);
                
                chunk2D.generated = true;
                chunk2D.generating = false;
                chunk2D.built = false;
            }
        }

        for (int cx = 0; cx < sx; cx++)
        {
            for (int cz = 0; cz < sz; cz++)
            {
                generator.BuildColumn(cx, cz);
            }
        }

        RealWorldTerrain.phaseComplete = true;
    }

    public static void GenerateChunk2D(TerraMap map, TerraVol.Chunk2D chunk2D, RealWorldTerrainItem item, int totalX, int totalZ)
    {
        int cx = chunk2D.position.x;
        int cz = chunk2D.position.y;
        bool atLimit = cx <= map.minGX || cz <= map.minGZ || cx >= map.maxGX || cz >= map.maxGZ;
        float ground = -2;
        float underground = map.minY * Chunk.SIZE_Y;

        float x1 = item.area.x;
        float x2 = item.area.x + item.area.width;
        float z1 = item.area.y;
        float z2 = item.area.y + item.area.height;
        float minElevation = item.minElevation;
        float elevationRange = item.maxElevation - minElevation;
        float scaledRange = elevationRange / item.size.y * map.blockSizeY;
        float nodataDepth = (RealWorldTerrain.prefs.nodataValue - minElevation) / scaledRange;

        Debug.Log(elevationRange + "    " + scaledRange + "    " + minElevation + "    " + item.maxElevation);

        for (int z = 0; z < Chunk.SIZE_Z; z++)
        {
            int worldZ = cz * Chunk.SIZE_Z + z;
            float pz = Mathf.Lerp(z1, z2, worldZ / (float)totalZ);

            for (int x = 0; x < Chunk.SIZE_X; x++)
            {
                int worldX = cx * Chunk.SIZE_X + x;
                float px = Mathf.Lerp(x1, x2, worldX / (float)totalX);

                float elevation = GetElevation(px, pz);
                float h1;
                if (Math.Abs(elevation - float.MinValue) > 0.1f) h1 = (elevation - minElevation) / scaledRange;
                else h1 = nodataDepth;

                int worldY = ((int)h1) + Chunk.SIZE_Y_TOTAL * 4;

                for (; worldY > h1; worldY--)
                {
                    float isovalue = 0;
                    if (worldY > h1)
                    {
                        isovalue += Mathf.Pow(worldY - h1, 1.2f) * 0.05f;
                    }

                    if (isovalue < 1.0f)
                    {
                        isovalue = Mathf.Clamp(isovalue, -1.0f, 1.0f);
                        GenerateBlock(map, worldX, worldY, worldZ, isovalue);
                    }
                }

                for (; worldY >= ground; worldY--)
                {
                    float isovalue = 0;

                    isovalue -= Mathf.Pow(h1 - worldY, 1.5f) * 0.01f;

                    if (isovalue < 1.0f)
                    {
                        isovalue = Mathf.Clamp(isovalue, -1.0f, 1.0f);
                        GenerateBlock(map, worldX, worldY, worldZ, isovalue);
                    }
                }

                // Sous-sol
                for (; worldY >= underground; worldY--)
                {
                    GenerateBlock(map, worldX, worldY, worldZ, -1.0f);
                }
            }
        }

        // Restore player actions
        if (!map.limitSize || !atLimit)
            TerraVol.WorldRecorder.Instance.RestoreColumn(cx, cz);
    }

    private static void GenerateBlock(TerraMap map, int worldX, int worldY, int worldZ, float isovalue)
    {
        TerraVol.Block block = map.terraVolEnhance.OnBlockGenerateBeforeInThread(new TerraVol.Vector3i(worldX, worldY, worldZ));
        if (block == null) block = map.defaultBlock;
        TerraVol.Vector3i loc = RealWorldTerrainTerraVolBridge.ChunkToLocalPosition(worldX, worldY, worldZ);
        map.SetBlock(new TerraVol.BlockData(block, loc, isovalue), worldX, worldY, worldZ);
    }
#endif

    public static float GetElevation(float x, float y)
    {
        if (prefs.elevationProvider == RealWorldTerrainElevationProvider.SRTM)
            return RealWorldTerrainElevationSRTM.GetSRTMElevation(x, y);
        return RealWorldTerrainElevationBing.GetBingElevation(x, y);
    }

    public static void GetElevationRange(out float minEl, out float maxEl)
    {
        if (prefs.elevationProvider == RealWorldTerrainElevationProvider.SRTM) RealWorldTerrainElevationSRTM.GetSRTMElevationRange(out minEl, out maxEl);
        else RealWorldTerrainElevationBing.GetBingElevationRange(out minEl, out maxEl);
    }

    public virtual float GetElevationValue(float x, float y)
    {
        return float.MinValue;
    }

    private short GetFixedValue(int X, int Y)
    {
        short v = GetValue(X, Y);
        if (v == short.MinValue) v = 0;
        return v;
    }


    protected float GetSmoothElevation(float xs1, float xs2, float xp1, float ys1, float ys2, float yp1, int ix, int iy,
        float ox, int ixp1, float oy, int iyp1, float oxy, int ixs1, int iys1, int ixp2, int iyp2)
    {
        float result = xs1 * xs2 * xp1 * ys1 * ys2 * yp1 * 0.25f * GetFixedValue(ix, iy);
        result -= ox * xp1 * xs2 * ys1 * ys2 * yp1 * 0.25f * GetFixedValue(ixp1, iy);
        result -= oy * xs1 * xs2 * xp1 * yp1 * ys2 * 0.25f * GetFixedValue(ix, iyp1);
        result += oxy * xp1 * xs2 * yp1 * ys2 * 0.25f * GetFixedValue(ixp1, iyp1);
        result -= ox * xs1 * xs2 * ys1 * ys2 * yp1 / 12.0f * GetFixedValue(ixs1, iy);
        result -= oy * xs1 * xs2 * xp1 * ys1 * ys2 / 12.0f * GetFixedValue(ix, iys1);
        result += oxy * xs1 * xs2 * yp1 * ys2 / 12.0f * GetFixedValue(ixs1, iyp1);
        result += oxy * xp1 * xs2 * ys1 * ys2 / 12.0f * GetFixedValue(ixp1, iys1);
        result += ox * xs1 * xp1 * ys1 * ys2 * yp1 / 12.0f * GetFixedValue(ixp2, iy);
        result += oy * xs1 * xs2 * xp1 * ys1 * yp1 / 12.0f * GetFixedValue(ix, iyp2);
        result += oxy * xs1 * xs2 * ys1 * ys2 / 36.0f * GetFixedValue(ixs1, iys1);
        result -= oxy * xs1 * xp1 * yp1 * ys2 / 12.0f * GetFixedValue(ixp2, iyp1);
        result -= oxy * xp1 * xs2 * ys1 * yp1 / 12.0f * GetFixedValue(ixp1, iyp2);
        result -= oxy * xs1 * xp1 * ys1 * ys2 / 36.0f * GetFixedValue(ixp2, iys1);
        result -= oxy * xs1 * xs2 * ys1 * yp1 / 36.0f * GetFixedValue(ixs1, iyp2);
        result += oxy * xs1 * xp1 * ys1 * yp1 / 36.0f * GetFixedValue(ixp2, iyp2);
        return result;
    }

    protected short GetValue(int X, int Y)
    {
        X = Mathf.Clamp(X, 0, mapSize - 1);
        Y = Mathf.Clamp(Y, 0, mapSize - 1);
        short v = heightmap[X, Y];
        return v;
    }

    private static void InitTData(RealWorldTerrainItem item)
    {
        tdata = item.terrain.terrainData;
        tdata.baseMapResolution = prefs.baseMapResolution;
        tdata.SetDetailResolution(prefs.detailResolution, prefs.resolutionPerPatch);
        tdata.heightmapResolution = prefs.heightmapResolution;
        tdata.size = item.size;
        if (tdataHeightmap == null) tdataHeightmap = new float[tdata.heightmapHeight, tdata.heightmapWidth];
        hasUnderwater = false;
        curDepth = 0;
        lastX = 0;
    }

    private static bool IsSingleDistance(int X, int Y, bool ignoreLeft, bool ignoreTop)
    {
        int l1 = tdata.heightmapWidth;
        int l2 = tdata.heightmapHeight;
        if (!ignoreTop && Y > 0 && tdataHeightmap[Y - 1, X] != float.MinValue) return true;
        if (Y < l1 - 1 && tdataHeightmap[Y + 1, X] != float.MinValue) return true;
        if (!ignoreLeft && X > 0 && tdataHeightmap[Y, X - 1] != float.MinValue) return true;
        if (X < l2 - 1 && tdataHeightmap[Y, X + 1] != float.MinValue) return true;
        return false;
    }
}