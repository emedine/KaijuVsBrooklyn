/*     INFINITY CODE 2013-2015      */
/*   http://www.infinity-code.com   */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class RealWorldTerrainElevationBing : RealWorldTerrainElevation
{
    public static string key;

    private static string bingFolder;

    private string filename;
    private float x1;
    private float x2;
    private float y1;
    private float y2;

    private RealWorldTerrainElevationBing(float x1, float y1, float x2, float y2)
    {
        this.x1 = x1;
        this.y1 = y1;
        this.x2 = x2;
        this.y2 = y2;

        filename = Path.Combine(RealWorldTerrainEditorUtils.heightmapCacheFolder, String.Format("bing_{0}x{1}x{2}x{3}x{4}.rwt", x1, y1, x2, y2, mapSize));
        if (!File.Exists(filename)) Download();
    }

    public override bool Contains(float X, float Y)
    {
        return X >= x1 && X <= x2 && Y >= y2 && Y <= y1;
    }

    private void Download()
    {
        int s = mapSize / 32;

        for (int x = 0; x < s; x++)
        {
            float sx = (x2 - x1) / s * x + x1;
            float ex = (x2 - x1) / s * (x + 1) + x1;
            ex = (ex - sx) / 33 * 32 + sx;

            for (int y = 0; y < s; y++)
            {
                float sy = (y1 - y2) / s * y + y2;
                float ey = (y1 - y2) / s * (y + 1) + y2;
                ey = (ey - sy) / 33 * 32 + sy;

                string partFilename = Path.Combine(bingFolder,
                    String.Format("bing_{0}x{1}x{2}x{3}x{4}.json", sx, sy, ex, ey, mapSize));
                if (!File.Exists(partFilename))
                {
                    string partURL =
                        string.Format(
                            "http://dev.virtualearth.net/REST/v1/Elevation/Bounds?bounds={0},{1},{2},{3}&rows=32&cols=32&key={4}",
                            sy, sx, ey, ex, key);
                    RealWorldTerrainDownloader.Add(partURL, partFilename, RealWorldTerrainDownloadType.data,
                        "Elevation maps", 4800, false);
                }
            }
        }
    }

    public static void Dispose()
    {
        if (elevations != null)
        {
            foreach (RealWorldTerrainElevation elevation in elevations) elevation.heightmap = null;
        }
        elevations = null;
    }

    public static float GetBingElevation(float x, float y)
    {
        if (x < prefs.coordinatesFrom.x) x = prefs.coordinatesFrom.x;
        if (x > prefs.coordinatesTo.x) x = prefs.coordinatesTo.x;
        if (y < prefs.coordinatesTo.y) y = prefs.coordinatesTo.y;
        if (y > prefs.coordinatesFrom.y) y = prefs.coordinatesFrom.y;

        if (lastElevation != null && lastElevation.Contains(x, y)) return lastElevation.GetElevationValue(x, y);
        RealWorldTerrainElevation elevation = elevations.FirstOrDefault(i => i.Contains(x, y));
        if (elevation == null) return float.MinValue;
        lastElevation = elevation;
        return elevation.GetElevationValue(x, y);
    }

    public static void GetBingElevationRange(out float minEl, out float maxEl)
    {
        minEl = float.MaxValue;
        maxEl = float.MinValue;

        foreach (RealWorldTerrainElevationBing elevation in elevations)
        {
            foreach (short h in elevation.heightmap)
            {
                if (h < minEl) minEl = h;
                if (h > maxEl) maxEl = h;
            }
        }

        if (minEl > prefs.nodataValue) minEl = prefs.nodataValue;
    }

    public override float GetElevationValue(float x, float y)
    {
        float ex = (x - x1) / (x2 - x1);
        float ey = 1 - (y - y1) / (y2 - y1);

        float cx = ex * (mapSize - 1);
        float cy = ey * (mapSize - 1);

        int ix = (int)cx;
        int iy = (int)cy;

        if (prefs.nodataValue != 0 && GetValue(ix, iy) == short.MinValue) return float.MinValue;

        float ox = cx - ix;
        float oy = cy - iy;

        return GetSmoothElevation(ox - 1, ox - 2, ox + 1, oy - 1, oy - 2, oy + 1, ix, iy, ox, ix + 1, oy, iy + 1,
            ox * oy, ix - 1, iy - 1, ix + 2, iy + 2);
    }

    public static void Init()
    {
        mapSize = (prefs.heightmapResolution - 1);

        Vector2 distance = RealWorldTerrainUtils.DistanceBetweenPoints(prefs.coordinatesFrom, prefs.coordinatesTo);
        while (mapSize > 32 && distance.x / mapSize < 0.005f && distance.y / mapSize < 0.005f) mapSize /= 2;

        bingFolder = Path.Combine(RealWorldTerrainEditorUtils.heightmapCacheFolder, "BingParts");
        if (!Directory.Exists(bingFolder)) Directory.CreateDirectory(bingFolder);

        float sizeX = (prefs.coordinatesTo.x - prefs.coordinatesFrom.x) / prefs.terrainCount.x;
        float sizeY = (prefs.coordinatesFrom.y - prefs.coordinatesTo.y) / prefs.terrainCount.y;

        elevations = new List<RealWorldTerrainElevation>();

        for (int x = 0; x < prefs.terrainCount.x; x++)
        {
            float cx = sizeX * x + prefs.coordinatesFrom.x;

            for (int y = 0; y < prefs.terrainCount.y; y++)
            {
                float cy = sizeY * y + prefs.coordinatesTo.y;
                elevations.Add(new RealWorldTerrainElevationBing(cx, cy + sizeY, cx + sizeX, cy));
            }
        }
    }

    public static bool Load()
    {
        for (int i = 0; i < elevations.Count; i++)
        {
            RealWorldTerrainElevationBing elevation = (RealWorldTerrainElevationBing)elevations[i];
            RealWorldTerrain.phaseProgress = i / (float)elevations.Count;
            if (File.Exists(elevation.filename)) elevation.LoadHeightmap();
            else if (!elevation.TryLoadParts())
            {
                Debug.LogError("Cant load elevation data");
                return false;
            }
        }
        return true;
    }

    private void LoadHeightmap()
    {
        heightmap = new short[mapSize, mapSize];
        FileStream fs = File.OpenRead(filename);
        const int size = 1000000;
        int c = 0;
        RealWorldTerrain.phaseProgress = 0;
        do
        {
            byte[] buffer = new byte[size];
            int count = fs.Read(buffer, 0, size);

            for (int i = 0; i < count; i += 2)
            {
                heightmap[c % mapSize, c / mapSize] = BitConverter.ToInt16(buffer, i);
                c++;
            }
            if (!RealWorldTerrain.isCapturing)
            {
                fs.Close();
                return;
            }
        }
        while (fs.Position != fs.Length);

        fs.Close();
        GC.Collect();
    }

    private bool TryLoadParts()
    {
        int s = mapSize / 32;
        heightmap = new short[mapSize, mapSize];

        for (int x = 0; x < s; x++)
        {
            float sx = (x2 - x1) / s * x + x1;
            float ex = (x2 - x1) / s * (x + 1) + x1;
            ex = (ex - sx) / 33 * 32 + sx;

            for (int y = 0; y < s; y++)
            {
                float sy = (y1 - y2) / s * y + y2;
                float ey = (y1 - y2) / s * (y + 1) + y2;
                ey = (ey - sy) / 33 * 32 + sy;

                string partFilename = Path.Combine(bingFolder, String.Format("bing_{0}x{1}x{2}x{3}x{4}.json", sx, sy, ex, ey, mapSize));
                if (!File.Exists(partFilename)) return false;

                string json = File.ReadAllText(partFilename);
                Match match = Regex.Match(json, "\"elevations\":\\[(.*?)]");
                if (match.Success)
                {
                    short[] heights = match.Groups[1].Value.Split(new[] {','}).Select(v => short.Parse(v)).ToArray();

                    for (int i = 0; i < heights.Length; i++)
                    {
                        int hx = i % 32;
                        int hy = i / 32;
                        heightmap[x * 32 + hx, y * 32 + hy] = heights[i];
                    }
                }
                else return false;
            }
        }

        FileStream wfs = File.OpenWrite(filename);
        BinaryWriter bw = new BinaryWriter(wfs);

        for (int y = 0; y < mapSize; y++)
        {
            for (int x = 0; x < mapSize; x++) bw.Write(heightmap[x, y]);
        }

        bw.Close();
        wfs.Close();

        for (int x = 0; x < s; x++)
        {
            float sx = (x2 - x1) / s * x + x1;
            float ex = (x2 - x1) / s * (x + 1) + x1;

            for (int y = 0; y < s; y++)
            {
                float sy = (y1 - y2) / s * y + y2;
                float ey = (y1 - y2) / s * (y + 1) + y2;

                string partFilename = Path.Combine(bingFolder, String.Format("bing_{0}x{1}x{2}x{3}x{4}.json", sx, sy, ex, ey, mapSize));
                File.Delete(partFilename);
            }
        }

        return true;
    }
}