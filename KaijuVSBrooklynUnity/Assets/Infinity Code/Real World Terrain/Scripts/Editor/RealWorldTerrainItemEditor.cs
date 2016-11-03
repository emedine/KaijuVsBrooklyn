/*     INFINITY CODE 2013-2015      */
/*   http://www.infinity-code.com   */

using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof (RealWorldTerrainItem))]
public class RealWorldTerrainItemEditor : Editor
{
    private RealWorldTerrainItem item;
    private TerrainData terrainData;
    private Texture2D texture;

    public static void DrawLocationInfo(RealWorldTerrainMonoBase item)
    {
        GUILayout.Label("Top-Left: ");
        GUILayout.Label("  Latitude: " + item.topLeft.y);
        GUILayout.Label("  Longitude: " + item.topLeft.x);
        EditorGUILayout.Space();
        GUILayout.Label("Bottom-Right: ");
        GUILayout.Label("  Latitude: " + item.bottomRight.y);
        GUILayout.Label("  Longitude: " + item.bottomRight.x);
        EditorGUILayout.Space();
    }

// ReSharper disable once UnusedMember.Local

    public static void Export(RealWorldTerrainItem item, string filename = "")
    {
        if (string.IsNullOrEmpty(filename))
        {
            filename = EditorUtility.SaveFilePanel("Export heightmap", Application.dataPath, item.name + ".png", "png");
            if (string.IsNullOrEmpty(filename)) return;
        }

        TerrainData terrainData = item.terrainData;
        int hw = terrainData.heightmapWidth;
        int hh = terrainData.heightmapHeight;
        float[,] hm = terrainData.GetHeights(0, 0, hw, hh);
        Color[] colors = new Color[hw * hh];

        for (int y = 0; y < hh; y++)
        {
            int hy = y * hw;
            for (int x = 0; x < hw; x++)
            {
                float v = hm[x, y];
                colors[hy + x] = new Color(v, v, v);
            }
        }

        Texture2D textureHM = new Texture2D(hw, hh);
        textureHM.SetPixels(colors);
        textureHM.Apply();
        File.WriteAllBytes(filename, textureHM.EncodeToPNG());
    }

    private void Import()
    {
        string filename = EditorUtility.OpenFilePanel("Import heightmap", Application.dataPath, "png");
        if (string.IsNullOrEmpty(filename)) return;

        Texture2D textureHM = new Texture2D(32, 32);
        textureHM.LoadImage(File.ReadAllBytes(filename));

        if (textureHM.width != textureHM.height)
        {
            EditorUtility.DisplayDialog("Error.", "The width of the image is not equal to the height.", "OK");
            return;
        }

        if (textureHM.width != terrainData.heightmapWidth)
        {
            bool res = EditorUtility.DisplayDialog("Warning.",
                "Image size is not equal to the height map size. Change the size of the height map?", "Change", "Cancel");
            if (!res) return;
            terrainData.heightmapResolution = textureHM.width;
        }

        int hw = terrainData.heightmapWidth;
        int hh = terrainData.heightmapHeight;
        float[,] hm = new float[hw, hh];
        Color[] colors = textureHM.GetPixels();

        for (int y = 0; y < hh; y++)
        {
            int hy = y * hw;
            for (int x = 0; x < hw; x++) hm[x, y] = colors[hy + x].grayscale;
        }

        terrainData.SetHeights(0, 0, hm);
    }

// ReSharper disable once UnusedMember.Local
    private void OnDisable()
    {
        EditorApplication.update -= OnUpdate;
    }

// ReSharper disable once UnusedMember.Local
    private void OnEnable()
    {
        EditorApplication.update += OnUpdate;
        item = (RealWorldTerrainItem) target;
        if (item.resultType == RealWorldTerrainResultType.terrain) terrainData = item.terrainData;
    }

    public override void OnInspectorGUI()
    {
        DrawLocationInfo(item);

        Texture2D currentTexture = null;
        if (item.resultType == RealWorldTerrainResultType.terrain)
        {
            if (terrainData == null) return;
#if !RTP
            if (terrainData.splatPrototypes.Length > 0) texture = terrainData.splatPrototypes[0].texture;
            currentTexture = texture;
#endif
        }
        else if (item.resultType == RealWorldTerrainResultType.mesh)
        {
            Renderer renderer = item.GetComponentInChildren<Renderer>();
            if (renderer != null && renderer.sharedMaterial != null && renderer.sharedMaterial.mainTexture != null)
                texture = (Texture2D)renderer.sharedMaterial.mainTexture;
        }
#if T4M
        else if (item.resultType == RealWorldTerrainResultType.T4M)
        {
            if (item.renderer != null && item.renderer.sharedMaterial != null && item.renderer.sharedMaterial.GetTexture("_Splat0") != null)
                texture = (Texture2D) item.renderer.sharedMaterial.GetTexture("_Splat0");
        }
#endif
        
#if !RTP
        texture = (Texture2D) EditorGUILayout.ObjectField("Texture: ", texture, typeof (Texture2D), true);
#endif
        if (texture != currentTexture)
        {
            if (item.resultType == RealWorldTerrainResultType.terrain)
            {
#if !RTP
                if (texture == null) terrainData.splatPrototypes = new SplatPrototype[0];
                else
                {
                    SplatPrototype sp = new SplatPrototype
                    {
                        texture = texture,
                        tileSize = new Vector2(Mathf.Round(terrainData.size.x), Mathf.Round(terrainData.size.z))
                    };
                    terrainData.splatPrototypes = new[] { sp };
                }
#endif
            }
            else if (item.resultType == RealWorldTerrainResultType.mesh)
            {
                Renderer[] rs = item.GetComponentsInChildren<Renderer>();
                foreach (Renderer r in rs) if (r.sharedMaterial != null) r.sharedMaterial.mainTexture = texture;
            }
#if T4M
            else if (item.resultType == RealWorldTerrainResultType.T4M)
            {
                if (item.renderer != null && item.renderer.sharedMaterial != null) item.renderer.sharedMaterial.SetTexture("_Splat0", texture);
            }
#endif
        }

        if (item.resultType == RealWorldTerrainResultType.terrain)
        {
            if (GUILayout.Button("Regenerate Textures")) RealWorldTerrain.OpenWindow(RealWorldTerrainGenerateType.texture, item);
            if (GUILayout.Button("Regenerate Additional")) RealWorldTerrain.OpenWindow(RealWorldTerrainGenerateType.additional, item);
            if (GUILayout.Button("Generate Grass from Texture")) RealWorldTerrainGrassGenerator.OpenWindow(item);
            if (GUILayout.Button("Generate SplatPrototypes from Texture"))
                RealWorldTerrainSplatPrototypeGenerator.OpenWindow(item);

            if (GUILayout.Button("Export heightmap")) Export(item);
            if (GUILayout.Button("Import heightmap")) Import();
        }
    }

    private void OnUpdate()
    {
        if (item != null && item.needUpdate)
        {
            item.needUpdate = false;
            Repaint();
        }
    }
}