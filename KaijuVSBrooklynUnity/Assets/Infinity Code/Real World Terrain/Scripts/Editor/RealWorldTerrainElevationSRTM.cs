/*     INFINITY CODE 2013-2015      */
/*   http://www.infinity-code.com   */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using InfinityCode.Zip;
using UnityEngine;

public class RealWorldTerrainElevationSRTM: RealWorldTerrainElevation
{
    private const string server = "http://srtm.csi.cgiar.org/SRT-ZIP/SRTM_v41/SRTM_Data_ArcASCII/";

    private readonly string arcFilename;
    private readonly string filename;
    private readonly string heightmapFilename;
    private readonly string heightmapFilenameC;
    private readonly int x;
    private readonly int y;

    public bool unziped;

    private byte[] heightmapArchive;

    public RealWorldTerrainElevationSRTM(int X, int Y)
    {
        x = X;
        y = Y;

        int ax = Mathf.FloorToInt((x + 180) / 5.0f + 1);
        int ay = Mathf.FloorToInt((90 - y) / 5.0f - 6);
        filename = String.Format("srtm_{0}_{1}", (ax > 9) ? ax.ToString() : "0" + ax,
            (ay > 9) ? ay.ToString() : "0" + ay);
        arcFilename = Path.Combine(RealWorldTerrainEditorUtils.heightmapCacheFolder, filename + ".zip");
        heightmapFilename = Path.Combine(RealWorldTerrainEditorUtils.heightmapCacheFolder, filename + ".asc");
        heightmapFilenameC = Path.Combine(RealWorldTerrainEditorUtils.heightmapCacheFolder, filename + ".rwt");
        RealWorldTerrainDownloader.Add(server + filename + ".zip", arcFilename, RealWorldTerrainDownloadType.www,
            "Elevation maps", 25000000);
    }

    public override bool Contains(float X, float Y)
    {
        float offX = X - x;
        float offY = Y - y;
        return offX >= 0 && offX <= 5 && offY >= 0 && offY <= 5;
    }

    public static void Dispose()
    {
        if (elevations != null)
        {
            foreach (RealWorldTerrainElevationSRTM elevation in elevations)
            {
                if (File.Exists(elevation.heightmapFilename))
                {
                    FileInfo info = new FileInfo(elevation.heightmapFilename);
                    if (info.Length == 0) RealWorldTerrainUtils.SafeDeleteFile(elevation.heightmapFilename);
                }

                elevation.heightmap = null;
                elevation.heightmapArchive = null;
            }
        }

        lastX = lastY = 0;
        meshItemCreated = false;
        multiMeshCreated = false;

        lastElevation = null;
    }

    public override float GetElevationValue(float ex, float ey)
    {
        ex = (ex - x) / 5.0f;
        ey = 1 - (ey - y) / 5.0f;

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

    public static float GetSRTMElevation(float x, float y)
    {
        if (lastElevation != null && lastElevation.Contains(x, y)) return lastElevation.GetElevationValue(x, y);
        RealWorldTerrainElevation elevation = elevations.FirstOrDefault(i => i.Contains(x, y));
        if (elevation == null) return float.MinValue;
        lastElevation = elevation;
        return elevation.GetElevationValue(x, y);
    }

    public static void GetSRTMElevationRange(out float minEl, out float maxEl)
    {
        minEl = float.MaxValue;
        maxEl = float.MinValue;

        int cx = prefs.terrainCount.x * (prefs.heightmapResolution - 1) + 1;
        int cy = prefs.terrainCount.y * (prefs.heightmapResolution - 1) + 1;

        const int maxV = 4097;
        if (cx > maxV && cx > cy)
        {
            float sv = maxV / (float)cx;
            cx = maxV;
            cy = Mathf.RoundToInt(cy * sv);
        }
        else if (cy > maxV)
        {
            float sv = maxV / (float)cy;
            cy = maxV;
            cx = Mathf.RoundToInt(cx * sv);
        }

        for (int x = 0; x < cx; x++)
        {
            float tx = Mathf.Lerp(prefs.coordinatesFrom.x, prefs.coordinatesTo.x,
                x / (float)cx);

            for (int y = 0; y < cy; y++)
            {
                float ty = Mathf.Lerp(prefs.coordinatesTo.y, prefs.coordinatesFrom.y,
                    y / (float)cy);
                float el = GetElevation(tx, ty);
                if (Math.Abs(el - float.MinValue) > 0.1f)
                {
                    if (el < minEl) minEl = el;
                    if (el > maxEl) maxEl = el;
                }
            }
        }

        if (minEl > prefs.nodataValue) minEl = prefs.nodataValue;
    }

    public static void Init(Vector2 cStart, Vector2 cEnd, Vector3 range)
    {
        mapSize = 6001;
        int countElX = 0;
        for (int x = Mathf.FloorToInt(cStart.x); x <= cEnd.x; x += 5)
        {
            countElX++;
            for (int y = Mathf.FloorToInt(cStart.y); y <= cEnd.y; y += 5)
                elevations.Add(new RealWorldTerrainElevationSRTM(x - 180, 90 - y));
        }
        float dsx = range.x / countElX / (prefs.heightmapResolution - 1) *
                    ((prefs.depthSharpness != 0) ? prefs.depthSharpness : 1);
        depthStep = dsx * 5000;
    }

    public void ParseHeightmap()
    {
        if (File.Exists(heightmapFilenameC))
        {
            ParseHeightmapC();
            return;
        }

        heightmap = new short[mapSize, mapSize];

        if (!File.Exists(heightmapFilename))
        {
            for (int hx = 0; hx < mapSize; hx++) for (int hy = 0; hy < mapSize; hy++) heightmap[hx, hy] = short.MaxValue;
            Debug.Log("Can not find the file:" + heightmapFilename);
            return;
        }

        FileStream fs = new FileStream(heightmapFilename, FileMode.Open);
        FileStream wfs = File.OpenWrite(heightmapFilenameC);
        BinaryWriter bw = new BinaryWriter(wfs);

        const int bufferSize = 1000000;
        List<byte> res = new List<byte>();
        int counter = 0;
        short nodata = 0;

        RealWorldTerrain.phaseProgress = 0;
        do
        {
            byte[] buffer = new byte[bufferSize];
            fs.Read(buffer, 0, bufferSize);

            foreach (byte b in buffer) ParseHeightmapBuffer(b, res, ref counter, bw, ref nodata);
            if (!RealWorldTerrain.isCapturing)
            {
                fs.Close();
                bw.Close();
                RealWorldTerrainUtils.SafeDeleteFile(heightmapFilenameC);
                return;
            }
            RealWorldTerrain.phaseProgress = fs.Position / (float)fs.Length;
        }
        while (fs.Position != fs.Length);

        fs.Close();
        bw.Close();

        GC.Collect();
    }

    private void ParseHeightmapBuffer(byte b, List<byte> res, ref int counter, BinaryWriter bw, ref short nodata)
    {
        if ((b > 0x2F && b < 0x3A) || b == 0x2D || b == 0x2E) res.Add(b);
        else
        {
            if (res.Count > 0)
            {
                counter++;
                if (counter == 6) nodata = Int16.Parse(Encoding.Default.GetString(res.ToArray()));
                else if (counter > 6)
                {
                    int index = counter - 7;
                    short el = Int16.Parse(Encoding.Default.GetString(res.ToArray()));
                    if (el == nodata) el = short.MinValue;
                    heightmap[index % mapSize, index / mapSize] = el;
                    bw.Write(el);
                }
                res.Clear();
            }
        }
    }

    private void ParseHeightmapC()
    {
        heightmap = new short[mapSize, mapSize];
        FileStream fs = File.OpenRead(heightmapFilenameC);
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
            RealWorldTerrain.phaseProgress = c / fs.Length * 2;
        }
        while (fs.Position != fs.Length);

        fs.Close();
        GC.Collect();
    }

    public void UnzipHeightmap()
    {
        if (File.Exists(heightmapFilename))
        {
            unziped = true;
            return;
        }

        if (!File.Exists(arcFilename))
        {
            Debug.Log("Can not find the file:" + arcFilename);
            unziped = true;
            return;
        }

        if (new FileInfo(arcFilename).Length == 0)
        {
            RealWorldTerrainUtils.SafeDeleteFile(arcFilename);

            RealWorldTerrain.CancelCapture();
            Debug.LogWarning("Error downloading elevation map.");
            return;
        }

        string localFN = filename + ".asc";

        Stream baseStream;
        if (heightmapArchive == null) baseStream = File.Open(arcFilename, FileMode.Open);
        else baseStream = new MemoryStream(heightmapArchive);
        ZipInputStream stream = new ZipInputStream(baseStream);

        ZipEntry entry;

        while ((entry = stream.GetNextEntry()) != null)
        {
            if (entry.Name == localFN)
            {
                byte[] buffer = new byte[entry.Size];
                stream.Read(buffer, (int)entry.Offset, (int)entry.Size);
                File.WriteAllBytes(heightmapFilename, buffer);

                stream.Close();
                heightmapArchive = null;
                unziped = true;
                GC.Collect();
                return;
            }
        }

        Debug.Log("Unzip failed. Try to re-download elevation map using Real World Terrain Helper.");
    }
}