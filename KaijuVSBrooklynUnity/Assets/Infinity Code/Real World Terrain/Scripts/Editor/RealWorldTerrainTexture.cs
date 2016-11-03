/*     INFINITY CODE 2013-2015      */
/*   http://www.infinity-code.com   */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class RealWorldTerrainTexture
{
    public static Color[] colors;
    public static bool ready;
    public static List<RealWorldTerrainTexture> reqTextures;
    public static List<RealWorldTerrainTexture> textures;

    private static bool allowGC;
    private static Texture2D generatedTexture;
    private static RealWorldTerrainTexture lastTexture;
    private static int lastX;
    private static int maxTextureLevel;
    private static int maxZoomLevel;
    private static string textureFilename;
    private static int textureWidth;
    private static int textureHeight;

    public bool exist;

    private readonly float ex;
    private readonly float ey;
    private readonly float offX;
    private readonly float offY;
    private readonly float x;
    private readonly float y;
    private readonly int zoom;
    private readonly string levelFolder;
    private readonly RealWorldTerrainVector2i tilePos;
    private string filename;
    private bool loaded;
    private string path;
    private Texture2D texture;

#if RTP
    public static List<Texture2D> rtpTextures;
#endif

    private static RealWorldTerrainPrefs prefs
    {
        get { return RealWorldTerrain.prefs; }
    }

    private bool downloaded
    {
        get { return File.Exists(path); }
    }

    private RealWorldTerrainTexture(float X, float Y, int ZOOM)
    {
        x = X;
        y = Y;
        zoom = ZOOM;

        tilePos = RealWorldTerrainUtils.LatLongToTile(x, y, zoom);
        Vector2 ep = RealWorldTerrainUtils.TileToLatLong(tilePos.x + 1, tilePos.y + 1, zoom);
        ex = ep.x;
        ey = ep.y;
        offX = ex - x;
        offY = ey - y;

        levelFolder = Path.Combine(RealWorldTerrainEditorUtils.textureCacheFolder, zoom.ToString());
        if (!Directory.Exists(levelFolder)) Directory.CreateDirectory(levelFolder);

        RealWorldTerrainTextureProvider provider = prefs.textureProvider;
        if (provider == RealWorldTerrainTextureProvider.google) InitGoogle();
        else if (provider == RealWorldTerrainTextureProvider.arcGIS) InitArcGIS();
        else if (provider == RealWorldTerrainTextureProvider.openStreetMap) InitOpenStreetMap();
        else if (provider == RealWorldTerrainTextureProvider.virtualEarth) InitVirtualEarth();
        else if (provider == RealWorldTerrainTextureProvider.nokia) InitNokia();
        else if (provider == RealWorldTerrainTextureProvider.mapQuest) InitMapQuest();
        else if (provider == RealWorldTerrainTextureProvider.custom) InitCustom();
    }

    private bool Contains(double X)
    {
        return X >= x && X <= ex;
    }

    private bool Contains(double X, double Y)
    {
        return X >= x && X <= ex && Y >= ey && Y <= y;
    }

    public void Dispose()
    {
        generatedTexture = null;
        lastTexture = null;
        lastX = 0;
        texture = null;
        textureFilename = string.Empty;
        colors = null;
    }

    public static void GenerateTexture(RealWorldTerrainItem item)
    {
        if (string.IsNullOrEmpty(textureFilename))
        {
            if (!ready) Prepare();

            lastX = 0;
            RealWorldTerrain.phaseProgress = 0;
            lastTexture = null;
            reqTextures = textures.Where(t => t.Intersects(item)).ToList();
            if (reqTextures.Count == 0)
            {
                RealWorldTerrain.phaseComplete = true;
                return;
            }
            maxZoomLevel = reqTextures.Max(t => t.zoom);
            textureWidth = prefs.textureSize.x;
            textureHeight = prefs.textureSize.y;

            if (colors == null) colors = new Color[textureHeight];

            if (prefs.reduceTextures && maxZoomLevel < maxTextureLevel)
            {
                for (int i = maxZoomLevel; i < maxTextureLevel; i++)
                {
                    if (textureWidth <= 128 || textureHeight <= 128) break;
                    textureWidth /= 2;
                    textureHeight /= 2;
                }
            }

            generatedTexture = new Texture2D(textureWidth, textureHeight);
            textureFilename = Path.Combine(RealWorldTerrain.container.folder,
                item.name + "r" + textureWidth + "x" + textureHeight + ".png");
        }

        const int offset = 4;
        float tsx = textureWidth - offset;
        float tsy = textureHeight - offset;

        float x1 = item.area.x;
        float y1 = item.area.y;
        float x2 = item.area.x + item.area.width;
        float y2 = item.area.y + item.area.height;

        float rx = x2 - x1;
        float ry = y1 - y2;

        long startTime = DateTime.Now.Ticks;

        for (int tx = lastX; tx < textureWidth; tx++)
        {
                
            double px = rx * (tx / (double)tsx) + x1;
            foreach (RealWorldTerrainTexture t in reqTextures) if (!t.Contains(px)) t.Unload();
            if (allowGC)
            {
                GC.Collect();
                allowGC = false;
            }

            for (int ty = 0; ty < textureHeight; ty++)
            {
                double py = ry * (ty / (double)tsy) + y2;
                colors[ty] = GetTextureColor(px, py);
            }

            generatedTexture.SetPixels(tx, 0, 1, textureHeight, colors);
            lastX = tx;
            RealWorldTerrain.phaseProgress = tx / (float) textureWidth;
            if (new TimeSpan(DateTime.Now.Ticks - startTime).TotalSeconds > 1) return;
        }

        foreach (RealWorldTerrainTexture t in reqTextures) t.Unload();
        reqTextures = null;

        generatedTexture.Apply();

        File.WriteAllBytes(textureFilename, generatedTexture.EncodeToPNG());

        AssetDatabase.Refresh();

#if UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7
        EditorUtility.UnloadUnusedAssets();
#else
        EditorUtility.UnloadUnusedAssetsImmediate();
#endif
        GC.Collect();

        generatedTexture = (Texture2D) AssetDatabase.LoadAssetAtPath(textureFilename, typeof (Texture2D));

        if (item.resultType == RealWorldTerrainResultType.terrain)
        {
#if !RTP
            Vector3 terrainSize = item.terrain.terrainData.size;

            Vector2 tileSize = new Vector2(terrainSize.x + terrainSize.x / tsx * 4,
                terrainSize.z + terrainSize.z / tsy * 4);

            Vector2 tileOffset = new Vector2(terrainSize.x / textureWidth / 2, terrainSize.z / textureHeight / 2);

            SplatPrototype sp = new SplatPrototype
            {
                texture = generatedTexture,
                tileSize = tileSize,
                tileOffset = tileOffset
            };

            item.terrain.terrainData.splatPrototypes = new[] { sp };
#else
            if (rtpTextures == null || rtpTextures.Count != 12)
            {
                rtpTextures = new List<Texture2D>();
                rtpTextures.Add((Texture2D)RealWorldTerrainEditorUtils.FindAndLoad("Dirt.png", typeof(Texture2D)));
                rtpTextures.Add((Texture2D)RealWorldTerrainEditorUtils.FindAndLoad("Dirt Height.png", typeof(Texture2D)));
                rtpTextures.Add((Texture2D)RealWorldTerrainEditorUtils.FindAndLoad("Dirt Normal.png", typeof(Texture2D)));
                rtpTextures.Add((Texture2D)RealWorldTerrainEditorUtils.FindAndLoad("Grass.png", typeof(Texture2D)));
                rtpTextures.Add((Texture2D)RealWorldTerrainEditorUtils.FindAndLoad("Grass Height.png", typeof(Texture2D)));
                rtpTextures.Add((Texture2D)RealWorldTerrainEditorUtils.FindAndLoad("Grass Normal.png", typeof(Texture2D)));
                rtpTextures.Add((Texture2D)RealWorldTerrainEditorUtils.FindAndLoad("GrassRock.png", typeof(Texture2D)));
                rtpTextures.Add((Texture2D)RealWorldTerrainEditorUtils.FindAndLoad("GrassRock Height.png", typeof(Texture2D)));
                rtpTextures.Add((Texture2D)RealWorldTerrainEditorUtils.FindAndLoad("GrassRock Normal.png", typeof(Texture2D)));
                rtpTextures.Add((Texture2D)RealWorldTerrainEditorUtils.FindAndLoad("Cliff.png", typeof(Texture2D)));
                rtpTextures.Add((Texture2D)RealWorldTerrainEditorUtils.FindAndLoad("Cliff Height.png", typeof(Texture2D)));
                rtpTextures.Add((Texture2D)RealWorldTerrainEditorUtils.FindAndLoad("Cliff Normal.png", typeof(Texture2D)));
            }

            SplatPrototype[] sps = new SplatPrototype[4];

            for (int i = 0; i < 4; i++)
            {
                sps[i] = new SplatPrototype { texture = rtpTextures[i * 3] };
            }

            item.terrain.terrainData.splatPrototypes = sps;

            ReliefTerrain reliefTerrain = item.gameObject.AddComponent<ReliefTerrain>() ?? item.gameObject.AddComponent<ReliefTerrain>();
            reliefTerrain.InitArrays();
            reliefTerrain.ColorGlobal = generatedTexture;
#endif
        }
        else if (item.resultType == RealWorldTerrainResultType.mesh)
        {
            Material mat = new Material(Shader.Find("Diffuse"));
            mat.mainTexture = generatedTexture;

            float oX = 4f / (item.textureSize.x - 4);
            float oY = 4f / (item.textureSize.y - 4);

            mat.SetTextureScale("_MainTex", new Vector2(1 - oX, 1 - oY)); 
            mat.SetTextureOffset("_MainTex", new Vector2(oX / 2, oY / 2));

            Renderer[] rs = item.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in rs) r.material = mat;

            string filename = Path.Combine(item.container.folder, item.name) + ".mat";
            AssetDatabase.CreateAsset(mat, filename);
            AssetDatabase.SaveAssets();
        }
#if T4M
        else if (item.resultType == RealWorldTerrainResultType.T4M)
        {
            Texture2D controlTexture2D = new Texture2D(512, 512);
            int bufferSize = 512 * 512;
            Color[] controlBuffer = new Color[bufferSize];
            Color clr = new Color(1, 0, 0, 0);
            for (int i = 0; i < bufferSize; i++) controlBuffer[i] = clr;
            controlTexture2D.SetPixels(controlBuffer);
            controlTexture2D.Apply(false);

            string controlTextureFilename = Path.Combine(RealWorldTerrain.container.folder,
                item.name + "r" + textureWidth + "x" + textureHeight + "_control.png");
            File.WriteAllBytes(controlTextureFilename, controlTexture2D.EncodeToPNG());

            AssetDatabase.Refresh();

            TextureImporter textureImporter = AssetImporter.GetAtPath(controlTextureFilename) as TextureImporter;
            if (textureImporter != null)
            {
                textureImporter.maxTextureSize = 512;
                textureImporter.isReadable = true;
                textureImporter.generateMipsInLinearSpace = false;
                textureImporter.textureFormat = TextureImporterFormat.ARGB32;
                AssetDatabase.ImportAsset(controlTextureFilename, ImportAssetOptions.ForceUpdate);
            }

            controlTexture2D = (Texture2D)AssetDatabase.LoadAssetAtPath(controlTextureFilename, typeof(Texture2D));
            
            Material mat = new Material(Shader.Find("T4MShaders/ShaderModel2/Diffuse/T4M 4 Textures"));
            mat.SetTexture("_Splat0", generatedTexture);
            mat.SetTexture("_Control", controlTexture2D);

            item.GetComponent<T4MObjSC>().T4MMaterial = mat;

            T4MPartSC[] parts = item.GetComponentsInChildren<T4MPartSC>();
            foreach (T4MPartSC t in parts) t.renderer.material = mat;

            string filename = Path.Combine(item.container.folder, item.name) + ".mat";
            AssetDatabase.CreateAsset(mat, filename);
            AssetDatabase.SaveAssets();
        }
#endif
#if TERRAVOL
        else if (item.resultType == RealWorldTerrainResultType.terraVol)
        {
            item.GetComponent<TerraMap>().blockSet.materials[0].mainTexture = generatedTexture;
        }
#endif
        
        RealWorldTerrain.phaseComplete = true;
        textureFilename = string.Empty;
        item.generatedTextures = true;
    }

    private Color GetColor(double X, double Y)
    {
        if (!loaded) LoadTexture();

        return texture.GetPixelBilinear(Mathf.Clamp01((float)((X - x) / offX)), Mathf.Clamp01((float)((ey - Y) / offY)));
    }

    private static Color GetTextureColor(double x, double y)
    {
        if (lastTexture != null && lastTexture.zoom == maxZoomLevel && lastTexture.Contains(x, y))
            return lastTexture.GetColor(x, y);

        int maxZoom = 0;
        RealWorldTerrainTexture at = null;

        foreach (RealWorldTerrainTexture t in reqTextures.Where(t => t.Contains(x, y)))
        {
            if (t.zoom >= maxZoom)
            {
                at = t;
                maxZoom = t.zoom;
            }
        }

        if (at != null)
        {
            lastTexture = at;
            return at.GetColor(x, y);
        }
        return Color.red;
    }

    public static bool Init(int level)
    {
        int needDownloadTextures = 0;
        maxTextureLevel = level;

        RealWorldTerrainDownloader.ClearTemp();

        textures = new List<RealWorldTerrainTexture>();
        for (int l = level; l > 4; l--)
        {
            RealWorldTerrainVector2i bt = RealWorldTerrainUtils.LatLongToTile(prefs.coordinatesFrom, l);
            RealWorldTerrainVector2i et = RealWorldTerrainUtils.LatLongToTile(prefs.coordinatesTo, l);

            for (int x = bt.x; x <= et.x; x++)
            {
                for (int y = bt.y; y <= et.y; y++)
                {
                    Vector2 bm = RealWorldTerrainUtils.TileToLatLong(x, y, l);
                    RealWorldTerrainTexture texture = new RealWorldTerrainTexture(bm.x, bm.y, l);
                    if (!texture.downloaded) needDownloadTextures += RealWorldTerrainUtils.AverageTextureSize;
                    textures.Add(texture);
                }
            }
        }

        if (prefs.textureProvider == RealWorldTerrainTextureProvider.google &&
            needDownloadTextures > RealWorldTerrainUtils.DownloadTextureLimit)
        {
            if (prefs.maxTextureLevel == 0) Init(level - 1);
            else
            {
                int sizeMB = Mathf.RoundToInt(needDownloadTextures / 1048576f);
                bool abort = EditorUtility.DisplayDialog("Warning",
                    String.Format("Need to download textures: ~{0} mb. You can be blocked. Abort?", sizeMB),
                    "Yes", "No");
                if (abort)
                {
                    textures = null;
                    RealWorldTerrainDownloader.ClearTemp();
                    return false;
                }
            }
        }

        RealWorldTerrainDownloader.ApplyTemp();
        return true;
    }

    private void InitArcGIS()
    {
        const string server =
            "http://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{0}/{1}/{2}";
        filename = "ag" + tilePos.x + "x" + tilePos.y + ".jpg";
        path = Path.Combine(levelFolder, filename);
        const long failedCRC = 3235296958;
        RealWorldTerrainDownloader.AddTemp(String.Format(server, zoom, tilePos.y, tilePos.x), path,
            RealWorldTerrainDownloadType.www, "Texture tiles", RealWorldTerrainUtils.AverageTextureSize, failedCRC);
    }

    private void InitCustom()
    {
        filename = "custom" + tilePos.x + "x" + tilePos.y + ".jpg";
        path = Path.Combine(levelFolder, filename);

        string url = Regex.Replace(prefs.textureProviderURL, @"{\w+}", ReplaceToken);
        RealWorldTerrainDownloader.AddTemp(url, path,
            RealWorldTerrainDownloadType.www, "Texture tiles", RealWorldTerrainUtils.AverageTextureSize);
    }

    private void InitGoogle()
    {
        string appendix = "";
        string server = "https://khm{0}.google.com/kh/v=196&src=app&x={1}&y={2}&z={3}&s=";

        if (prefs.textureType == RealWorldTerrainTextureType.terrain)
        {
            appendix = "t";
            server = "https://mts{0}.google.com/vt/src=app&x={1}&y={2}&z={3}&s=";
        }
        else if (prefs.textureType == RealWorldTerrainTextureType.relief)
        {
            appendix = "r";
            server = "https://mts{0}.google.com/vt/lyrs=t@131,r@216000000&src=app&x={1}&y={2}&z={3}&s=";
        }

        filename = tilePos.x + "x" + tilePos.y + appendix + ".jpg";
        path = Path.Combine(levelFolder, filename);
        RealWorldTerrainDownloader.AddTemp(String.Format(server, 0, tilePos.x, tilePos.y, zoom), path,
            RealWorldTerrainDownloadType.www, "Texture tiles", RealWorldTerrainUtils.AverageTextureSize);
    }

    private void InitMapQuest()
    {
        const string server = "http://mtile01.mqcdn.com/tiles/1.0.0/vy/sat/{0}/{1}/{2}.png";
        const long failedCRC = 839257317;
        filename = "mq" + tilePos.x + "x" + tilePos.y + ".png";
        path = Path.Combine(levelFolder, filename);
        RealWorldTerrainDownloader.AddTemp(String.Format(server, zoom, tilePos.x, tilePos.y), path,
            RealWorldTerrainDownloadType.www, "Texture tiles", RealWorldTerrainUtils.AverageTextureSize, failedCRC);
    }

    private void InitNokia()
    {
        const string server =
            "http://{0}.maps.nlp.nokia.com/maptile/2.1/maptile/newest/satellite.day/{1}/{2}/{3}/256/png8?lg=ENG&app_id=xWVIueSv6JL0aJ5xqTxb&app_code=djPZyynKsbTjIUDOBcHZ2g";
        filename = "n" + tilePos.x + "x" + tilePos.y + ".png";
        path = Path.Combine(levelFolder, filename);
        RealWorldTerrainDownloader.AddTemp(String.Format(server, 1, zoom, tilePos.x, tilePos.y), path,
            RealWorldTerrainDownloadType.www, "Texture tiles", RealWorldTerrainUtils.AverageTextureSize);
    }

    private void InitOpenStreetMap()
    {
        const string server =
            "http://a.tile.openstreetmap.org/{0}/{1}/{2}.png";
        filename = "osm" + tilePos.x + "x" + tilePos.y + ".jpg";
        path = Path.Combine(levelFolder, filename);
        RealWorldTerrainDownloader.AddTemp(String.Format(server, zoom, tilePos.x, tilePos.y), path,
            RealWorldTerrainDownloadType.www, "Texture tiles", RealWorldTerrainUtils.AverageTextureSize);
    }

    private void InitVirtualEarth()
    {
        const string server = "http://ak.t{0}.tiles.virtualearth.net/tiles/a{1}.jpeg?g=1457&n=z";
        filename = "ve" + tilePos.x + "x" + tilePos.y + ".jpg";
        path = Path.Combine(levelFolder, filename);
        string quad = RealWorldTerrainUtils.TileToQuadKey(tilePos.x, tilePos.y, zoom);
        RealWorldTerrainDownloader.AddTemp(String.Format(server, 0, quad), path, RealWorldTerrainDownloadType.www,
            "Texture tiles", RealWorldTerrainUtils.AverageTextureSize);
    }

    private bool Intersects(RealWorldTerrainMonoBase item)
    {
        float x1 = item.topLeft.x;
        float y1 = item.bottomRight.y;
        float x2 = item.bottomRight.x;
        float y2 = item.topLeft.y;

        bool xIn = (x >= x1 && x <= x2) || (ex >= x1 && ex <= x2);
        bool yIn = (y >= y1 && y <= y2) || (ey >= y1 && ey <= y2);
        bool xOut = (x <= x1 && ex >= x2);
        bool yOut = (ey <= y1 && y >= y2);

        return (xIn && (yIn || yOut)) || (xOut && (yIn || yOut));
    }

    private void Load()
    {
        if (texture != null || !downloaded) return;
        if (new FileInfo(path).Length == 0)
        {
            RealWorldTerrainUtils.SafeDeleteFile(path);
            return;
        }

        if (zoom > maxZoomLevel) maxZoomLevel = zoom;

        exist = true;
    }

    private void LoadTexture()
    {
        texture = new Texture2D(RealWorldTerrainUtils.tileSize, RealWorldTerrainUtils.tileSize);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.LoadImage(File.ReadAllBytes(path));
        loaded = true;
    }

    private static void Prepare()
    {
        if (ready) return;
        foreach (RealWorldTerrainTexture texture in textures) texture.Load();
        textures.RemoveAll(t => !t.exist);
        ready = true;
    }

    private string ReplaceToken(Match match)
    {
        string v = match.Value.ToLower().Trim('{', '}');
        if (v == "zoom") return zoom.ToString();
        if (v == "x") return tilePos.x.ToString();
        if (v == "y") return tilePos.y.ToString();
        if (v == "quad") return RealWorldTerrainUtils.TileToQuadKey(tilePos.x, tilePos.y, zoom);
        return v;
    }

    private void Unload()
    {
        if (!loaded) return;
        UnityEngine.Object.DestroyImmediate(texture);
        texture = null;
        loaded = false;
        allowGC = true;
    }
}