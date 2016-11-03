/*     INFINITY CODE 2013-2015      */
/*   http://www.infinity-code.com   */

using UnityEditor;
using UnityEngine;

public class RealWorldTerrainFeatureRequest : EditorWindow
{
    private string description = "";
    private string email = "";
    private string senderName = "";
    private static RealWorldTerrainFeatureRequest wnd;

// ReSharper disable once UnusedMember.Local
    private void OnEnable()
    {
        if (EditorPrefs.HasKey("RWT_ReporterName")) senderName = EditorPrefs.GetString("RWT_ReporterName");
        if (EditorPrefs.HasKey("RWT_ReporterMail")) email = EditorPrefs.GetString("RWT_ReporterMail");
    }

// ReSharper disable once UnusedMember.Local
    private void OnGUI()
    {
        senderName = EditorGUILayout.TextField("Name: ", senderName);
        email = EditorGUILayout.TextField("Email: ", email);
        GUILayout.Label("Message:");
        description = EditorGUILayout.TextArea(description, GUILayout.ExpandHeight(true));

        if (GUILayout.Button("Send")) OnSend();
    }

    private void OnSend()
    {
        if (RealWorldTerrainReporter.SendReport(senderName, email, "Feature request", description))
        {
            EditorUtility.DisplayDialog("Message was sent",
                "Thanks for the feature request. We will contact you shortly.", "OK");
            wnd.Close();
        }
        else
        {
            EditorUtility.DisplayDialog("Failed",
                "When you send an error occurred. Please email us directly at \"support@infinity-code.com\".", "OK");
        }
    }

    public static void OpenWindow()
    {
        wnd = GetWindow<RealWorldTerrainFeatureRequest>(true, "Feature request");
    }
}