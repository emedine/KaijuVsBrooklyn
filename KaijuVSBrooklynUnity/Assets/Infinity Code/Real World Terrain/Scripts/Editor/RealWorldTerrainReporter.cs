/*     INFINITY CODE 2013-2015      */
/*   http://www.infinity-code.com   */

using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEngine;

public class RealWorldTerrainReporter : EditorWindow
{
    private string description = "";
    private string email = "";
    private bool includePrefs = true;
    private string senderName = "";
    private static RealWorldTerrainReporter wnd;

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
        includePrefs = GUILayout.Toggle(includePrefs, "Send the component settings.");
        GUILayout.Label("Description:");
        description = EditorGUILayout.TextArea(description, GUILayout.ExpandHeight(true));

        if (GUILayout.Button("Send")) OnSend();
    }

    private void OnSend()
    {
        if (SendReport(senderName, email, "Bug report", description, includePrefs))
        {
            EditorUtility.DisplayDialog("Message was sent",
                "Thanks for the bug report. We will contact you shortly.", "OK");
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
        wnd = GetWindow<RealWorldTerrainReporter>(true, "Report an error");
    }

    public static bool SendReport(string senderName, string email, string title, string description,
        bool includePrefs = false)
    {
        EditorPrefs.SetString("RWT_ReporterName", senderName);
        EditorPrefs.SetString("RWT_ReporterMail", email);

        WebClient client = new WebClient();
        NameValueCollection inputs = new NameValueCollection
        {
            {"key", "SCRwgNiVWe"},
            {"name", senderName},
            {"email", email},
            {"title", title},
            {"description", description}
        };

        if (includePrefs)
        {
            RealWorldTerrainPrefs prefs = new RealWorldTerrainPrefs();
            prefs.Load();
            inputs.Add("prefs", prefs.ToXML(new XmlDocument()).OuterXml);
        }
        byte[] response = client.UploadValues("http://infinity-code.com/products_update/real-world-terrain/Report.php", inputs);
        string responseStr = Encoding.UTF8.GetString(response);
        return responseStr == "Report send success.";
    }
}