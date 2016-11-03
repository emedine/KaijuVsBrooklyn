/*     INFINITY CODE 2013-2015      */
/*   http://www.infinity-code.com   */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class RealWorldTerrainEditorUtils
{
    private static string _cacheFolder;
    private static string _heightmapCacheFolder;
    private static string _osmCacheFolder;
    private static string _textureCacheFolder;
    private static string _updateCacheFolder;

    private static string cacheFolder
    {
        get
        {
            if (String.IsNullOrEmpty(_cacheFolder))
            {
                string cache = RealWorldTerrainPrefs.LoadPref("CacheFolder", "");
                if (cache == "")
                {
                    DirectoryInfo parent = new DirectoryInfo(Application.dataPath).Parent;
                    if (parent != null) _cacheFolder = Path.Combine(parent.ToString(), "RWT_Cache");
                }
                else _cacheFolder = cache;
            }
            if (!Directory.Exists(_cacheFolder)) Directory.CreateDirectory(_cacheFolder);
            return _cacheFolder;
        }
    }

    public static string heightmapCacheFolder
    {
        get
        {
            if (String.IsNullOrEmpty(_heightmapCacheFolder))
                _heightmapCacheFolder = Path.Combine(cacheFolder, "Heightmaps");
            if (!Directory.Exists(_heightmapCacheFolder)) Directory.CreateDirectory(_heightmapCacheFolder);
            return _heightmapCacheFolder;
        }
    }

    public static string osmCacheFolder
    {
        get
        {
            if (String.IsNullOrEmpty(_osmCacheFolder)) _osmCacheFolder = Path.Combine(cacheFolder, "OSM");
            if (!Directory.Exists(_osmCacheFolder)) Directory.CreateDirectory(_osmCacheFolder);
            return _osmCacheFolder;
        }
    }

    public static string textureCacheFolder
    {
        get
        {
            if (String.IsNullOrEmpty(_textureCacheFolder)) _textureCacheFolder = Path.Combine(cacheFolder, "Textures");
            if (!Directory.Exists(_textureCacheFolder)) Directory.CreateDirectory(_textureCacheFolder);
            return _textureCacheFolder;
        }
    }

    public static string updateCacheFolder
    {
        get
        {
            if (String.IsNullOrEmpty(_updateCacheFolder)) _updateCacheFolder = Path.Combine(cacheFolder, "Updates");
            if (!Directory.Exists(_updateCacheFolder)) Directory.CreateDirectory(_updateCacheFolder);
            return _updateCacheFolder;
        }
    }

    public static void ClearFoldersCache()
    {
        _cacheFolder = String.Empty;
        _heightmapCacheFolder = String.Empty;
        _osmCacheFolder = String.Empty;
        _textureCacheFolder = String.Empty;
        _updateCacheFolder = String.Empty;
    }

    public static Object FindAndLoad(string filename, Type type)
    {
#if !UNITY_WEBPLAYER
        string[] files = Directory.GetFiles("Assets", filename, SearchOption.AllDirectories);
        if (files.Length > 0) return AssetDatabase.LoadAssetAtPath(files[0], type);
#endif
        return null;
    }

    public static Material FindMaterial(string materialName)
    {
        try
        {
            string materialPath = "Assets" +
                                  Directory.GetFiles(Application.dataPath, materialName, SearchOption.AllDirectories)[0]
                                      .Substring(Application.dataPath.Length).Replace('\\', '/');
            return (Material) AssetDatabase.LoadAssetAtPath(materialPath, typeof (Material));
        }
        catch
        {
            Debug.LogWarning("Can not find the material: " + materialName);
            return new Material(Shader.Find("Diffuse"));
        }
    }

    public static void GeneratePreviewTexture(TerrainData tdata, ref Texture2D previewTexture)
    {
        if (previewTexture != null) return;

        previewTexture = new Texture2D(1, 1);
        previewTexture.SetPixel(0, 0, Color.red);
        previewTexture.Apply();
        List<SplatPrototype> sps = tdata.splatPrototypes.ToList();
        SplatPrototype previewSp = new SplatPrototype
        {
            texture = previewTexture,
            tileSize = new Vector2(tdata.size.x, tdata.size.z)
        };
        sps.Add(previewSp);
        tdata.splatPrototypes = sps.ToArray();
    }

    public static Vector3 GlobalToLocal(float x, float z, Vector3 scale)
    {
        return
            Vector3.Scale(
                new Vector3(x - RealWorldTerrain.prefs.coordinatesFrom.x, 0, z - RealWorldTerrain.prefs.coordinatesTo.y),
                scale);
    }

    public static Vector3 GlobalToLocal(Vector3 point, Vector3 scale)
    {
        Vector3 local = GlobalToLocal(point.x, point.z, scale);
        local.y = point.y;
        return local;
    }

    public static Vector3 GlobalToLocalWithElevation(Vector3 point, Vector3 scale, Vector3 offset = default(Vector3))
    {
        float elevation = RealWorldTerrainElevation.GetElevation(point.x, point.z);
        if (elevation == float.MinValue) elevation = RealWorldTerrain.prefs.nodataValue;
        return GlobalToLocal(new Vector3(point.x, elevation, point.z), scale) - offset;
    }

    public static float NormalizeElevation(RealWorldTerrainContainer container, float elevation)
    {
        float elevationRange = container.maxElevation - container.minElevation;
        float scaledRange = elevationRange / container.size.y;
        elevation = (elevation - container.minElevation) / scaledRange;
        return elevation;
    }
}