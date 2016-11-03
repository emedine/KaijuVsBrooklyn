/*     INFINITY CODE 2013-2015      */
/*   http://www.infinity-code.com   */

using System.IO;
using UnityEditor;
using UnityEngine;

public class RealWorldTerrainClearCache : EditorWindow
{
    private bool clearOSM = false;
    private bool clearSRTM = false;
    private bool clearTexture = false;
    private bool clearTextureErrorOnly = false;
    private bool clearUpdates = false;

    private long osmSize;
    private long srtmSize;
    private long textureSize;
    private long updateSize;

    private void Clear()
    {
        if (clearSRTM) RealWorldTerrainUtils.SafeDeleteDirectory(RealWorldTerrainEditorUtils.heightmapCacheFolder);
        if (clearOSM) RealWorldTerrainUtils.SafeDeleteDirectory(RealWorldTerrainEditorUtils.osmCacheFolder);
        if (clearTexture)
        {
            if (!clearTextureErrorOnly)
                RealWorldTerrainUtils.SafeDeleteDirectory(RealWorldTerrainEditorUtils.textureCacheFolder);
            else
            {
                string[] files = Directory.GetFiles(RealWorldTerrainEditorUtils.textureCacheFolder, "*.err",
                    SearchOption.AllDirectories);
                foreach (string file in files) RealWorldTerrainUtils.SafeDeleteFile(file);
            }
        }
        if (clearUpdates) RealWorldTerrainUtils.SafeDeleteDirectory(RealWorldTerrainEditorUtils.updateCacheFolder);

        EditorUtility.DisplayDialog("Complete", "Clear cache complete.", "OK");

        Close();
    }

    private void OnEnable()
    {
        osmSize = RealWorldTerrainUtils.GetDirectorySize(RealWorldTerrainEditorUtils.osmCacheFolder);
        srtmSize = RealWorldTerrainUtils.GetDirectorySize(RealWorldTerrainEditorUtils.heightmapCacheFolder);
        textureSize = RealWorldTerrainUtils.GetDirectorySize(RealWorldTerrainEditorUtils.textureCacheFolder);
        updateSize = RealWorldTerrainUtils.GetDirectorySize(RealWorldTerrainEditorUtils.updateCacheFolder);
    }

    private string FormatSize(long size)
    {
        if (size > 10485760) return size / 1048576 + " MB";
        if (size > 1024) return (size / 1048576f).ToString("0.000") + " MB";
        return size + " B";

    }

// ReSharper disable once UnusedMember.Local
    private void OnGUI()
    {
        clearSRTM = GUILayout.Toggle(clearSRTM, "Clear SRTM cache (" + FormatSize(srtmSize) + ")");
        clearTexture = GUILayout.Toggle(clearTexture, "Clear texture cache (" + FormatSize(textureSize) + ")");
        if (clearTexture)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            clearTextureErrorOnly = GUILayout.Toggle(clearTextureErrorOnly, "Errors only");
            GUILayout.EndHorizontal();
        }
        clearOSM = GUILayout.Toggle(clearOSM, "Clear OSM cache (" + FormatSize(osmSize) + ")");
        clearUpdates = GUILayout.Toggle(clearUpdates, "Clear updates (" + FormatSize(updateSize) + ")");

        if (GUILayout.Button("Clear")) Clear();
    }

    public static void OpenWindow()
    {
        RealWorldTerrainClearCache wnd = GetWindow<RealWorldTerrainClearCache>(true, "Clear cache");
        DontDestroyOnLoad(wnd);
    }
}