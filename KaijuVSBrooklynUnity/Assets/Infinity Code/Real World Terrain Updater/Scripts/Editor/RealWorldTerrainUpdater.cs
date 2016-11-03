/*     INFINITY CODE 2013-2015      */
/*   http://www.infinity-code.com   */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class RealWorldTerrainUpdater : EditorWindow 
{
    private const string packageID = "Real World Terrain";
    private const string assetPrefix = "RealWorldTerrain";
    private const string lastVersionKey = assetPrefix + "LastVersion";
    private const string lastVersionCheckKey = assetPrefix + "LastVersionCheck";
    private const string channelKey = assetPrefix + "UpdateChannel";
    private const string invoiceNumberKey = assetPrefix + "InvoiceNumber";

    public static bool hasNewVersion = false;

    private static RealWorldTerrainUpdateChannel channel = RealWorldTerrainUpdateChannel.stable;
    private string invoiceNumber;
    private Vector2 scrollPosition;
    private List<RealWorldTerrainUpdateItem> updates;
    private static string lastVersionID;

    private void CheckNewVersions()
    {
        if (string.IsNullOrEmpty(invoiceNumber))
        {
            EditorUtility.DisplayDialog("Error", "Please enter the Invoice Number.", "OK");
            return;
        }

        int inum;

        if (!int.TryParse(invoiceNumber, out inum))
        {
            EditorUtility.DisplayDialog("Error", "Wrong Invoice Number.", "OK");
            return;
        }

        SavePrefs();

        string updateKey = GetUpdateKey();
        GetUpdateList(updateKey);
    }

    public static void CheckNewVersionAvailable()
    {
        if (EditorPrefs.HasKey(lastVersionKey))
        {
            lastVersionID = EditorPrefs.GetString(lastVersionKey);

            if (CompareVersions())
            {
                hasNewVersion = true;
                return;
            }
        }

        const long ticksInHour = 36000000000;

        if (EditorPrefs.HasKey(lastVersionCheckKey))
        {
            long lastVersionCheck = EditorPrefs.GetInt(lastVersionCheckKey) * ticksInHour;
            if (DateTime.Now.Ticks - lastVersionCheck < 24 * ticksInHour)
            {
                return;
            }
        }

        EditorPrefs.SetInt(lastVersionCheckKey, (int)(DateTime.Now.Ticks / ticksInHour));

        if (EditorPrefs.HasKey(channelKey))
            channel = (RealWorldTerrainUpdateChannel)EditorPrefs.GetInt(channelKey);
        else channel = RealWorldTerrainUpdateChannel.stable;
        if (channel == RealWorldTerrainUpdateChannel.stablePrevious) channel = RealWorldTerrainUpdateChannel.stable;

        WebClient client = new WebClient();
         
        client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
        client.UploadDataCompleted += delegate(object sender, UploadDataCompletedEventArgs response)
        {
            if (response.Error != null)
            {
                Debug.Log("Real World Terrain Updater: " + response.Error.Message);
                return;
            }

            string version = Encoding.UTF8.GetString(response.Result);

            try
            {
                string[] vars = version.Split(new[] { '.' });
                string[] vars2 = new string[4];
                vars2[0] = vars[0];
                vars2[1] = int.Parse(vars[1].Substring(0, 2)).ToString();
                vars2[2] = int.Parse(vars[1].Substring(2, 2)).ToString();
                vars2[3] = int.Parse(vars[1].Substring(4, 4)).ToString();
                version = string.Join(".", vars2);
            }
            catch (Exception)
            {
                Debug.Log("Real World Terrain Updater: Bad response");
                return;
            }
            
            lastVersionID = version;

            hasNewVersion = CompareVersions();
            EditorApplication.update += SetLastVersion;
        };
        client.UploadDataAsync(new Uri("http://infinity-code.com/products_update/getlastversion.php"), "POST", Encoding.UTF8.GetBytes("c=" + (int)channel + "&package=" + WWW.EscapeURL(packageID)));
    }

    private static bool CompareVersions()
    {
        double v1 = GetDoubleVersion(RealWorldTerrain.version);
        double v2 = GetDoubleVersion(lastVersionID);
        return v1 < v2;
    }

    private static double GetDoubleVersion(string v)
    {
        string[] vs = v.Split(new[] {"."}, StringSplitOptions.RemoveEmptyEntries);
        if (vs[1].Length < 2) vs[1] = "0" + vs[1];
        if (vs[2].Length < 2) vs[2] = "0" + vs[2];
        if (vs[3].Length < 4)
        {
            vs[3] = "000" + vs[3];
            vs[3] = vs[3].Substring(vs[3].Length - 4, 4);
        }
        v = vs[0] + "." + vs[1] + vs[2] + vs[3];
        double result;
        if (!double.TryParse(v, out result)) result = 1;
        return result;
    }

    private static void SetLastVersion()
    {
        EditorPrefs.SetString(lastVersionKey, lastVersionID);
        EditorApplication.update -= SetLastVersion;
    }

    private string GetUpdateKey()
    {
        WebClient client = new WebClient();
        client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
        string updateKey = client.UploadString("http://infinity-code.com/products_update/getupdatekey.php",
            "key=" + invoiceNumber + "&package=" + WWW.EscapeURL(packageID));

        return updateKey;
    }

    private void GetUpdateList(string updateKey)
    {
        WebClient client = new WebClient();
        client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";

        string response;

        try
        {
            response = client.UploadString("http://infinity-code.com/products_update/checkupdates.php", "k=" + WWW.EscapeURL(updateKey) + "&v=" + RealWorldTerrain.version + "&c=" + (int)channel);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(response);
            updates = new List<RealWorldTerrainUpdateItem>();

            foreach (XmlNode node in doc.DocumentElement.ChildNodes) updates.Add(new RealWorldTerrainUpdateItem(node));
        }
        catch
        {
        }
    }

    private void OnEnable()
    {
        if (EditorPrefs.HasKey(invoiceNumberKey))
            invoiceNumber = EditorPrefs.GetString(invoiceNumberKey);
        else invoiceNumber = "";

        if (EditorPrefs.HasKey(channelKey)) 
            channel = (RealWorldTerrainUpdateChannel)EditorPrefs.GetInt(channelKey);
        else channel = RealWorldTerrainUpdateChannel.stable;
    }

    private void OnDestroy()
    {
        SavePrefs();
    }

    private void OnGUI()
    {
        invoiceNumber = EditorGUILayout.TextField("Invoice Number:", invoiceNumber).Trim(new[] { ' ' });
        channel = (RealWorldTerrainUpdateChannel) EditorGUILayout.EnumPopup("Channel:", channel);
        GUILayout.Label("Current version: " + RealWorldTerrain.version);

        if (GUILayout.Button("Check new versions")) CheckNewVersions();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        if (updates != null)
        {
            foreach (RealWorldTerrainUpdateItem update in updates) update.Draw();
            if (updates.Count == 0) GUILayout.Label("No updates");
        }

        EditorGUILayout.EndScrollView();
    }

    [MenuItem("Window/Infinity Code/Real World Terrain/Check Updates", false, 1000)]
    public static void OpenWindow()
    {
        GetWindow<RealWorldTerrainUpdater>(false, "Real World Terrain Updater", true);
    }

    private void SavePrefs()
    {
        EditorPrefs.SetString(invoiceNumberKey, invoiceNumber);
        EditorPrefs.SetInt(channelKey, (int) channel);
    }
}

public class RealWorldTerrainUpdateItem
{
    private string version;
    private int type;
    private string changelog;
    private string download;
    private string date;

    private static GUIStyle _changelogStyle;
    private static GUIStyle _titleStyle;

    private static GUIStyle changelogStyle
    {
        get { return _changelogStyle ?? (_changelogStyle = new GUIStyle(EditorStyles.label) {wordWrap = true}); }
    }

    private static GUIStyle titleStyle
    {
        get
        {
            return _titleStyle ??
                   (_titleStyle = new GUIStyle(EditorStyles.boldLabel) {alignment = TextAnchor.MiddleCenter});
        }
    }

    public RealWorldTerrainUpdateItem(XmlNode node)
    {
        version = node["Version"].InnerXml;
        type = int.Parse(node["Type"].InnerXml);
        changelog = node["ChangeLog"].InnerXml;
        download = node["Download"].InnerXml;
        date = node["Date"].InnerXml;

        string[] vars = version.Split(new[] {'.'});
        string[] vars2 = new string[4];
        vars2[0] = vars[0];
        vars2[1] = int.Parse(vars[1].Substring(0, 2)).ToString();
        vars2[2] = int.Parse(vars[1].Substring(2, 2)).ToString();
        vars2[3] = int.Parse(vars[1].Substring(4, 4)).ToString();
        version = string.Join(".", vars2);
    }

    public void Draw()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("Version: " + version + " (" + typeStr + "). " + date, titleStyle);

        GUILayout.Label(changelog, changelogStyle);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Download"))
        {
            Process.Start("http://infinity-code.com/products_update/download.php?k=" + download);
        }

        if (GUILayout.Button("Copy download link", GUILayout.ExpandWidth(false)))
        {
            EditorGUIUtility.systemCopyBuffer = "http://infinity-code.com/products_update/download.php?k=" + download;
            EditorUtility.DisplayDialog("Success",
                "Download link is copied to the clipboard.\nOpen a browser and paste the link into the address bar.",
                "OK");
        }

        EditorGUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    public string typeStr
    {
        get { return Enum.GetName(typeof (RealWorldTerrainUpdateChannel), type); }
    }
}

public enum RealWorldTerrainUpdateChannel
{
    stable = 10,
    stablePrevious = 15,
    releaseCandidate = 20,
    beta = 30,
    alpha = 40,
    working = 50
}
