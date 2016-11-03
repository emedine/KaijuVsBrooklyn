/*     INFINITY CODE 2013-2015      */
/*   http://www.infinity-code.com   */

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class RealWorldTerrainSettings : EditorWindow
{
    private static string[] splineBendFindResult;
    private static RealWorldTerrainSettings wnd;

    private string bingAPI = "";
    private string customCacheFolder;
    private bool defaultCacheFolder = true;
    private bool hasThirdPartyAssets;
    private string resultName = "RealWorld Terrain";
    private Vector2 scrollPosition = Vector2.zero;
    private bool showCacheFolder = true;
    private bool showResultName = true;
    private bool showThirdPartyAssets = false;
    private bool showTokens;
    private static Assembly assembly;
    private bool appendIndex = true;

    // ReSharper disable once UnusedMember.Local

    private static void AddCompilerDirective(string key)
    {
        string currentDefinitions =
            PlayerSettings.GetScriptingDefineSymbolsForGroup(
                EditorUserBuildSettings.selectedBuildTargetGroup);

        string[] defs = currentDefinitions.Split(new[] { ';' }).Select(d => d.Trim(new[] { ' ' })).ToArray();

        if (defs.All(d => d != key))
        {
            ArrayUtility.Add(ref defs, key);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup,
                string.Join(";", defs));
        }
    }

    private static void DeleteCompilerDirective(string key)
    {
        string currentDefinitions =
            PlayerSettings.GetScriptingDefineSymbolsForGroup(
                EditorUserBuildSettings.selectedBuildTargetGroup);

        string[] defs = currentDefinitions.Split(new[] { ';' }).Select(d => d.Trim(new[] { ' ' })).ToArray();

        if (defs.Any(d => d == key))
        {
            ArrayUtility.Remove(ref defs, key);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup,
                string.Join(";", defs));
        }
    }

    private static Type FindType(string className)
    {
        if (assembly == null) assembly = typeof(RealWorldTerrainSettings).Assembly;
        return assembly.GetType(className);
    }

    private void OnEnable()
    {
        wnd = this;
        wnd.customCacheFolder = RealWorldTerrainPrefs.LoadPref("CacheFolder", "");
        wnd.defaultCacheFolder = wnd.customCacheFolder == "";
        wnd.resultName = RealWorldTerrainPrefs.LoadPref("ResultName", "RealWorld Terrain");
        wnd.appendIndex = RealWorldTerrainPrefs.LoadPref("AppendIndex", true);
        wnd.bingAPI = RealWorldTerrainPrefs.LoadPref("BingAPI", "");
        splineBendFindResult = Directory.GetFiles("Assets", "SplineBend.js", SearchOption.AllDirectories);
    }

// ReSharper disable once UnusedMember.Local
    private void OnGUI()
    {
        bool dirty = false;
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        OnGUICacheFolder();
        OnGUIResultName();

        GUILayout.BeginHorizontal();
        bingAPI = EditorGUILayout.TextField("Bing Maps API key: ", bingAPI);
        if (GUILayout.Button("Create Key", GUILayout.ExpandWidth(false))) Process.Start("http://msdn.microsoft.com/en-us/library/ff428642.aspx");
        GUILayout.EndHorizontal();

        OnGUIThirdParty(ref dirty);

        GUILayout.EndScrollView();

        if (GUILayout.Button("Save"))
        {
            if (defaultCacheFolder) RealWorldTerrainPrefs.DeletePref("CacheFolder");
            else RealWorldTerrainPrefs.SetPref("CacheFolder", customCacheFolder);

            if (resultName == "") RealWorldTerrainPrefs.DeletePref("ResultName");
            else RealWorldTerrainPrefs.SetPref("ResultName", resultName);

            RealWorldTerrainPrefs.SetPref("AppendIndex", appendIndex);

            if (bingAPI == "") RealWorldTerrainPrefs.DeletePref("BingAPI");
            else RealWorldTerrainPrefs.SetPref("BingAPI", bingAPI);

            RealWorldTerrainEditorUtils.ClearFoldersCache();

            Close();
        }

        if (dirty) Repaint();
    }

    private void OnGUIThirdParty(ref bool dirty)
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);
        showThirdPartyAssets = EditorGUILayout.Foldout(showThirdPartyAssets, "Third Party Assets:");
        if (showThirdPartyAssets)
        {
            hasThirdPartyAssets = false;

            OnBuildR(ref dirty);
            OnEasyRoads(ref dirty);
            OnRoadArchitect(ref dirty);
            OnRTP(ref dirty);
            OnSplineBend(ref dirty);
            OnT4C(ref dirty);
            OnTerraVol(ref dirty);

            if (!hasThirdPartyAssets) GUILayout.Label("Third Party Assets not found.");
        }

        EditorGUILayout.EndVertical();
    }

    private void OnGUICacheFolder()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);
        showCacheFolder = EditorGUILayout.Foldout(showCacheFolder, "Cache folder:");
        if (showCacheFolder)
        {
            defaultCacheFolder = GUILayout.Toggle(defaultCacheFolder, "{PROJECT FOLDER}/RWT_Cache");
            defaultCacheFolder = !GUILayout.Toggle(!defaultCacheFolder, "Custom cache folder");

            if (!defaultCacheFolder)
            {
                GUILayout.BeginHorizontal();
                customCacheFolder = EditorGUILayout.TextField("", customCacheFolder);
                GUI.SetNextControlName("BrowseButton");
                if (GUILayout.Button("Browse", GUILayout.ExpandWidth(false)))
                {
                    GUI.FocusControl("BrowseButton");
                    string newCustomFolder = EditorUtility.OpenFolderPanel("Select the folder for the cache.",
                        EditorApplication.applicationPath,
                        "");
                    if (!string.IsNullOrEmpty(newCustomFolder)) customCacheFolder = newCustomFolder;
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10);
        }
        EditorGUILayout.EndVertical();
    }

    private void OnGUIResultName()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);
        showResultName = EditorGUILayout.Foldout(showResultName, "Result GameObject name: ");
        if (showResultName)
        {
            resultName = EditorGUILayout.TextField("", resultName);
            GUILayout.Label("Example:\nRWT_{cx}x{cy} = RWT_4x4");

            DrawResultTokens();

            appendIndex = GUILayout.Toggle(appendIndex, "Append index if GameObject already exists?");
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawResultTokens()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);
        showTokens = EditorGUILayout.Foldout(showTokens, "Available tokens");
        if (showTokens)
        {
            GUILayout.Label("{tllat} - Top-Left latitude");
            GUILayout.Label("{tllng} - Top-Left longitude");
            GUILayout.Label("{brlat} - Bottom-Right latitude");
            GUILayout.Label("{brlng} - Bottom-Right longitude");
            GUILayout.Label("{cx} - Count X");
            GUILayout.Label("{cy} - Count Y");
            GUILayout.Label("{st} - Size type");
            GUILayout.Label("{me} - Max elevation");
            GUILayout.Label("{mu} - Max underwater depth");
            GUILayout.Label("{ds} - Depth shrapness");
            GUILayout.Label("{dr} - Detail resolution");
            GUILayout.Label("{rpp} - Resolution per patch");
            GUILayout.Label("{bmr} - Base map resolution");
            GUILayout.Label("{hmr} - Height map resolution");
            GUILayout.Label("{tp} - Texture provider");
            GUILayout.Label("{tw} - Texture width");
            GUILayout.Label("{th} - Texture height");
            GUILayout.Label("{tml} - Texture max level");
            GUILayout.Label("{ticks} - Current time ticks");
            GUILayout.Space(10);
        }
        EditorGUILayout.EndVertical();
    }

    private void OnBuildR(ref bool dirty)
    {
        if (FindType("BuildrEditModeEditor") == null) return;

#if !BUILDR
        if (GUILayout.Button("Enable BuildR"))
        {
            AddCompilerDirective("BUILDR");
            dirty = true;
        }
#else
        if (GUILayout.Button("Disable BuildR"))
        {
            DeleteCompilerDirective("BUILDR");
            dirty = true;
        }
#endif
        hasThirdPartyAssets = true;
    }

    private void OnEasyRoads(ref bool dirty)
    {
        if (FindType("NewEasyRoads3D") == null) return;
#if !EASYROADS
        if (GUILayout.Button("Enable EasyRoads3D"))
        {
            AddCompilerDirective("EASYROADS");
            dirty = true;
        }
#else
        if (GUILayout.Button("Disable EasyRoads3D"))
        {
            DeleteCompilerDirective("EASYROADS");
            dirty = true;
        }
#endif
        hasThirdPartyAssets = true;
    }

    private void OnRoadArchitect(ref bool dirty)
    {
        if (FindType("GSDRoadEditor") == null) return;
#if !ROADARCHITECT
        if (GUILayout.Button("Enable Road Architect"))
        {
            AddCompilerDirective("ROADARCHITECT");
            dirty = true;
        }
#else
        if (GUILayout.Button("Disable Road Architect"))
        {
            DeleteCompilerDirective("ROADARCHITECT");
            dirty = true;
        }
#endif
        hasThirdPartyAssets = true;
    }

    private void OnRTP(ref bool dirty)
    {
        if (FindType("RTP_LODmanagerEditor") == null) return;
#if !RTP
        if (GUILayout.Button("Enable Relief Terrain Pack"))
        {
            AddCompilerDirective("RTP");
            dirty = true;
        }
#else
        if (GUILayout.Button("Disable Relief Terrain Pack"))
        {
            DeleteCompilerDirective("RTP");
            dirty = true;
        }
#endif
        hasThirdPartyAssets = true;
    }

    private void OnSplineBend(ref bool dirty)
    {
        if (splineBendFindResult.Length == 0) return;
#if !SPLINEBEND
        if (GUILayout.Button("Enable SplineBend"))
        {
            AddCompilerDirective("SPLINEBEND");
            dirty = true;
        }
#else
        if (GUILayout.Button("Disable SplineBend"))
        {
            DeleteCompilerDirective("SPLINEBEND");
            dirty = true;
        }
#endif
        hasThirdPartyAssets = true;
    }

    private void OnT4C(ref bool dirty)
    {
        if (FindType("T4MSC") == null) return;
#if !T4M
        if (GUILayout.Button("Enable T4M"))
        {
            AddCompilerDirective("T4M");
            dirty = true;
        }
#else
        if (GUILayout.Button("Disable T4M"))
        {
            DeleteCompilerDirective("T4M");
            dirty = true;
        }
#endif
        hasThirdPartyAssets = true;
    }

    private void OnTerraVol(ref bool dirty)
    {
        if (FindType("TerraEditor") == null) return;
#if !TERRAVOL
        if (GUILayout.Button("Enable TerraVol"))
        {
            AddCompilerDirective("TERRAVOL");
            dirty = true;
        }
#else
        if (GUILayout.Button("Disable TerraVol"))
        {
            DeleteCompilerDirective("TERRAVOL");
            dirty = true;
        }
#endif
        hasThirdPartyAssets = true;
    }

    public static void OpenWindow()
    {
        wnd = GetWindow<RealWorldTerrainSettings>(false, "Settings");
    }
}
