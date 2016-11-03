/*     INFINITY CODE 2013-2015      */
/*   http://www.infinity-code.com   */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;
using Object = UnityEngine.Object;

public static class RealWorldTerrainUtils
{
    public const float arcSecond = 0.0001f;
    public const int AverageTextureSize = 20000;
    public const int DownloadTextureLimit = 90000000;
    public const int MaxElevation = 15000;
    public const short tileSize = 256;
    public const int Mb = 1048576;

    public const string overpassAPI = "http://overpass-api.de/api/interpreter?data=";

    public static float Angle2D(Vector2 point1, Vector2 point2)
    {
        return Mathf.Atan2((point2.y - point1.y), (point2.x - point1.x)) * Mathf.Rad2Deg;
    }

    public static float Angle2D(Vector3 point1, Vector3 point2)
    {
        return Mathf.Atan2((point2.z - point1.z), (point2.x - point1.x)) * Mathf.Rad2Deg;
    }

    public static float Angle2D(Vector3 point1, Vector3 point2, Vector3 point3, bool unsigned = true)
    {
        float angle1 = Angle2D(point1, point2);
        float angle2 = Angle2D(point2, point3);
        float angle = angle1 - angle2;
        if (angle > 180) angle -= 360;
        if (angle < -180) angle += 360;
        if (unsigned) angle = Mathf.Abs(angle);
        return angle;
    }

    public static float Angle2DRad(Vector3 point1, Vector3 point2, float offset)
    {
        return Mathf.Atan2((point2.z - point1.z), (point2.x - point1.x)) + offset * Mathf.Deg2Rad;
    }

    private static double Clip(double n, double minValue, double maxValue)
    {
        return Math.Min(Math.Max(n, minValue), maxValue);
    }

    public static GameObject CreateGameObject(MonoBehaviour parent, string name)
    {
        return CreateGameObject(parent.gameObject, name, Vector3.zero);
    }

    public static GameObject CreateGameObject(GameObject parent, string name)
    {
        return CreateGameObject(parent, name, Vector3.zero);
    }

    public static GameObject CreateGameObject(GameObject parent, string name, Vector3 position)
    {
        GameObject container = new GameObject(name);
        container.transform.parent = parent.transform;
        container.transform.localPosition = position;
        return container;
    }

    public static void DeleteGameObject(Transform current, string name)
    {
        for (int i = current.childCount - 1; i >= 0; i--)
        {
            Transform child = current.GetChild(i);
            if (child.name == name) Object.DestroyImmediate(child.gameObject);
            else DeleteGameObject(child, name);
        }
    }

    public static Vector2 DistanceBetweenPoints(Vector2 point1, Vector2 point2)
    {
        const float R = 6371;

        Vector2 range = point1 - point2;

        double scfY = Math.Sin(point1.y * Mathf.Deg2Rad);
        double sctY = Math.Sin(point2.y * Mathf.Deg2Rad);
        double ccfY = Math.Cos(point1.y * Mathf.Deg2Rad);
        double cctY = Math.Cos(point2.y * Mathf.Deg2Rad);
        double cX = Math.Cos(range.x * Mathf.Deg2Rad);
        double sizeX1 = Math.Abs(R * Math.Acos(scfY * scfY + ccfY * ccfY * cX));
        double sizeX2 = Math.Abs(R * Math.Acos(sctY * sctY + cctY * cctY * cX));
        float sizeX = (float)((sizeX1 + sizeX2) / 2.0);
        float sizeY = (float)(R * Math.Acos(scfY * sctY + ccfY * cctY));
        return new Vector2(sizeX, sizeY);
    }

    public static void ExportMesh(string filename, params MeshFilter[] mfs)
    {
        string res = "";
        int nextNormalIndex = 0;
        foreach (MeshFilter mf in mfs)
        {
            Mesh m = mf.sharedMesh;
#if UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7
            Material[] mats = mf.renderer.sharedMaterials;
#else
            Material[] mats = mf.GetComponent<Renderer>().sharedMaterials;
#endif

            res += "g " + mf.name + "\n";
            res = m.vertices.Aggregate(res, (current, v) => current + String.Format("v {0} {1} {2}\n", v.x, v.y, v.z)) +
                  "\n";
            res = m.normals.Aggregate(res, (current, v) => current + String.Format("vn {0} {1} {2}\n", v.x, v.y, v.z)) +
                  "\n";
            res = m.uv.Aggregate(res, (current, v) => current + String.Format("vt {0} {1}\n", v.x, v.y));

            for (int material = 0; material < m.subMeshCount; material++)
            {
                res += "\n" + "usemtl " + mats[material].name + "\n";
                res += "usemap " + mats[material].name + "\n";

                int[] triangles = m.GetTriangles(material);
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    res += String.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n", triangles[i] + 1 + nextNormalIndex,
                        triangles[i + 1] + 1 + nextNormalIndex, triangles[i + 2] + 1 + nextNormalIndex);
                }
            }

            res += "\n";
            nextNormalIndex += m.normals.Length;
        }

#if !UNITY_WEBPLAYER
        StreamWriter file = new StreamWriter(filename);
        file.Write(res);
#endif
    }

    public static long GetDirectorySize(DirectoryInfo folder)
    {
#if !UNITY_WEBPLAYER
        return folder.GetFiles().Sum(fi => fi.Length) + folder.GetDirectories().Sum(dir => GetDirectorySize(dir));
#else
        return 0;
#endif
    }

    public static long GetDirectorySize(string folderPath)
    {
        return GetDirectorySize(new DirectoryInfo(folderPath));
    }

    public static long GetDirectorySizeMB(string folderPath)
    {
        return GetDirectorySize(folderPath) / 1048576;
    }

    public static Vector2 GetIntersectionPointOfTwoLines(Vector2 p11, Vector2 p12, Vector2 p21, Vector2 p22,
        out int state)
    {
        state = -2;
        Vector2 result = new Vector2();
        float m = ((p22.x - p21.x) * (p11.y - p21.y) - (p22.y - p21.y) * (p11.x - p21.x));
        float n = ((p22.y - p21.y) * (p12.x - p11.x) - (p22.x - p21.x) * (p12.y - p11.y));

        float Ua = m / n;

        if (n == 0 && m != 0) state = -1;
        else if (m == 0 && n == 0) state = 0;
        else
        {
            result.x = p11.x + Ua * (p12.x - p11.x);
            result.y = p11.y + Ua * (p12.y - p11.y);

            if (((result.x >= p11.x || result.x <= p11.x) && (result.x >= p21.x || result.x <= p21.x))
                && ((result.y >= p11.y || result.y <= p11.y) && (result.y >= p21.y || result.y <= p21.y))) state = 1;
        }
        return result;
    }

    public static Vector2 GetIntersectionPointOfTwoLines(Vector3 p11, Vector3 p12, Vector3 p21, Vector3 p22,
        out int state)
    {
        return GetIntersectionPointOfTwoLines(new Vector2(p11.x, p11.z), new Vector2(p12.x, p12.z),
            new Vector2(p21.x, p21.z), new Vector2(p22.x, p22.z), out state);
    }

    public static Rect GetRectFromPoints(List<Vector3> points)
    {
        return new Rect
        {
            x = points.Min(p => p.x),
            y = points.Min(p => p.z),
            xMax = points.Max(p => p.x),
            yMax = points.Max(p => p.z)
        };
    }

    public static Color HexToColor(string hex)
    {
        byte r = Byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
        byte g = Byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
        byte b = Byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
        return new Color32(r, g, b, 255);
    }

    public static bool IsClockWise(Vector3 A, Vector3 B, Vector3 C)
    {
        return ((B.x - A.x) * (C.z - A.z) - (C.x - A.x) * (B.z - A.z)) > 0;
    }

    public static bool IsNullOrEmpty(this string str)
    {
        return string.IsNullOrEmpty(str);
    }

    public static bool IsPointInPolygon(List<Vector3> poly, float x, float y)
    {
        int i, j;
        bool c = false;
        for (i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
        {
            if ((((poly[i].z <= y) && (y < poly[j].z)) ||
                 ((poly[j].z <= y) && (y < poly[i].z))) &&
                (x < (poly[j].x - poly[i].x) * (y - poly[i].z) / (poly[j].z - poly[i].z) + poly[i].x))
                c = !c;
        }
        return c;
    }

    public static bool IsPointInPolygon(List<Vector3> poly, Vector3 point)
    {
        return IsPointInPolygon(poly, point.x, point.z);
    }

    public static Vector2 LanLongToFlat(Vector2 pos)
    {
        return new Vector2(Mathf.FloorToInt(pos.x / 5.0f) * 5 + 180, 90 - Mathf.FloorToInt(pos.y / 5.0f) * 5);
    }

    public static Vector2 LatLongToMercat(float x, float y)
    {
        float sy = Mathf.Sin(y * Mathf.Deg2Rad);
        return new Vector2((x + 180) / 360, 0.5f - Mathf.Log((1 + sy) / (1 - sy)) / (4 * Mathf.PI));
    }

    public static Vector2 LatLongToMercat(Vector2 pos)
    {
        return LatLongToMercat(pos.x, pos.y);
    }

    public static RealWorldTerrainVector2i LatLongToTile(float x, float y, int zoom)
    {
        Vector2 mPos = LatLongToMercat(x, y);
        uint mapSize = (uint) tileSize << zoom;
        int px = (int) Clip(mPos.x * mapSize + 0.5, 0, mapSize - 1);
        int py = (int) Clip(mPos.y * mapSize + 0.5, 0, mapSize - 1);
        int ix = px / tileSize;
        int iy = py / tileSize;

        return new RealWorldTerrainVector2i(ix, iy);
    }

    public static RealWorldTerrainVector2i LatLongToTile(Vector2 p, int zoom)
    {
        return LatLongToTile(p.x, p.y, zoom);
    }

    public static int Limit(int val, int min = 32, int max = 4096)
    {
        return Mathf.Clamp(val, min, max);
    }

    public static int LimitPowTwo(int val, int min = 32, int max = 4096)
    {
        return Mathf.Clamp(Mathf.ClosestPowerOfTwo(val), min, max);
    }

    public static string Replace(this string str, string[] oldValues, string newValue)
    {
        foreach (string oldValue in oldValues) str = str.Replace(oldValue, newValue);
        return str;
    }

    public static string Replace(this string str, string[] oldValues, string[] newValues)
    {
        for (int i = 0; i < oldValues.Length; i++) str = str.Replace(oldValues[i], newValues[i]);
        return str;
    }

    public static void SafeDeleteDirectory(string directoryName)
    {
        try
        {
#if !UNITY_WEBPLAYER
            Directory.Delete(directoryName, true);
#endif
        }
        catch
        {}
    }

    public static void SafeDeleteFile(string filename, int tryCount = 10)
    {
        while (tryCount-- > 0)
        {
            try
            {
                File.Delete(filename);
                break;
            }
            catch (Exception)
            {
                Thread.Sleep(10);
            }
        }
    }

    public static List<T> Splice<T>(this List<T> list, int offset, int count = 1)
    {
        List<T> newList = list.Skip(offset).Take(count).ToList();
        list.RemoveRange(offset, count);
        return newList;
    }

    public static Color StringToColor(string str)
    {
        str = str.ToLower();
        if (str == "black") return Color.black;
        if (str == "blue") return Color.blue;
        if (str == "cyan") return Color.cyan;
        if (str == "gray") return Color.gray;
        if (str == "green") return Color.green;
        if (str == "magenta") return Color.magenta;
        if (str == "red") return Color.red;
        if (str == "white") return Color.white;
        if (str == "yellow") return Color.yellow;

        try
        {
            string hex = (str + "000000").Substring(1, 6);
            byte[] cb =
                Enumerable.Range(0, hex.Length)
                    .Where(x => x % 2 == 0)
                    .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                    .ToArray();
            return new Color32(cb[0], cb[1], cb[2], 255);
        }
        catch
        {
            return Color.white;
        }
    }

    public static Vector2 TileToLatLong(int x, int y, int zoom)
    {
        double mapSize = tileSize << zoom;
        double lx = 360 * ((Clip(x * tileSize, 0, mapSize - 1) / mapSize) - 0.5);
        double ly = 90 -
                    360 * Math.Atan(Math.Exp(-(0.5 - (Clip(y * tileSize, 0, mapSize - 1) / mapSize)) * 2 * Math.PI)) /
                    Math.PI;
        return new Vector2((float) lx, (float) ly);
    }

    public static string TileToQuadKey(int x, int y, int zoom)
    {
        StringBuilder quadKey = new StringBuilder();
        for (int i = zoom; i > 0; i--)
        {
            char digit = '0';
            int mask = 1 << (i - 1);
            if ((x & mask) != 0) digit++;
            if ((y & mask) != 0)
            {
                digit++;
                digit++;
            }
            quadKey.Append(digit);
        }
        return quadKey.ToString();
    }

    public static string ToString2(this Vector3 v)
    {
        return string.Format("{{{0}, {1}, {2}}}", v.x, v.y, v.z);
    }

    public static string ToHex(this Color color)
    {
        return ((Color32) color).ToHex();
    }

    public static string ToHex(this Color32 color)
    {
        return color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2");
    }

    public static IEnumerable<int> Triangulate(List<Vector2> points)
    {
        List<int> indices = new List<int>();

        int n = points.Count;
        if (n < 3) return indices;

        int[] V = new int[n];
        if (TriangulateArea(points) > 0) for (int v = 0; v < n; v++) V[v] = v;
        else for (int v = 0; v < n; v++) V[v] = (n - 1) - v;

        int nv = n;
        int count = 2 * nv;
        for (int v = nv - 1; nv > 2;)
        {
            if ((count--) <= 0) return indices;

            int u = v;
            if (nv <= u) u = 0;
            v = u + 1;
            if (nv <= v) v = 0;
            int w = v + 1;
            if (nv <= w) w = 0;

            if (TriangulateSnip(points, u, v, w, nv, V))
            {
                int s, t;
                indices.Add(V[u]);
                indices.Add(V[v]);
                indices.Add(V[w]);
                for (s = v, t = v + 1; t < nv; s++, t++) V[s] = V[t];
                nv--;
                count = 2 * nv;
            }
        }

        indices.Reverse();
        return indices;
    }

    private static float TriangulateArea(List<Vector2> points)
    {
        int n = points.Count;
        float A = 0.0f;
        for (int p = n - 1, q = 0; q < n; p = q++)
        {
            Vector2 pval = points[p];
            Vector2 qval = points[q];
            A += pval.x * qval.y - qval.x * pval.y;
        }
        return (A * 0.5f);
    }

    private static bool TriangulateInsideTriangle(Vector2 A, Vector2 B, Vector2 C, Vector2 P)
    {
        float bp = (C.x - B.x) * (P.y - B.y) - (C.y - B.y) * (P.x - B.x);
        float ap = (B.x - A.x) * (P.y - A.y) - (B.y - A.y) * (P.x - A.x);
        float cp = (A.x - C.x) * (P.y - C.y) - (A.y - C.y) * (P.x - C.x);
        return ((bp >= 0.0f) && (cp >= 0.0f) && (ap >= 0.0f));
    }

    private static bool TriangulateSnip(List<Vector2> points, int u, int v, int w, int n, int[] V)
    {
        Vector2 A = points[V[u]];
        Vector2 B = points[V[v]];
        Vector2 C = points[V[w]];
        if (Mathf.Epsilon > (((B.x - A.x) * (C.y - A.y)) - ((B.y - A.y) * (C.x - A.x)))) return false;
        for (int p = 0; p < n; p++)
        {
            if (p == u || p == v || p == w) continue;
            if (TriangulateInsideTriangle(A, B, C, points[V[p]])) return false;
        }
        return true;
    }
}

#if TERRAVOL
public static class RealWorldTerrainTerraVolBridge
{
    public static TerraVol.Chunk2D TerraMap2DGetCreate(this TerraMap map, TerraVol.Vector2i pos)
    {
        return map.TerraMap2D.GetCreate(pos);
    }

    public static TerraVol.Vector3i ChunkToLocalPosition(int pointX, int pointY, int pointZ)
    {
        return Chunk.ToLocalPosition(pointX, pointY, pointZ);
    }
}
#endif