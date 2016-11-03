﻿/*     INFINITY CODE 2013-2015      */
/*   http://www.infinity-code.com   */

using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof (RealWorldTerrainMeta))]
// ReSharper disable UnusedMember.Global
public class RealWorldTerrainMetaEditor : Editor
// ReSharper restore UnusedMember.Global
{
    public override void OnInspectorGUI()
    {
        RealWorldTerrainMeta meta = (RealWorldTerrainMeta) target;
        if (meta.metaInfo == null) return;

        if (meta.center != Vector2.zero && GUILayout.Button("Open Street View"))
        {
            Process.Start(string.Format("https://maps.google.com?layer=c&cbll={0},{1}", meta.center.y, meta.center.x));
        }

        if (meta.hasURL && GUILayout.Button("Open URL")) Process.Start(meta.metaInfo.Where(m => m.title == "url").ToArray()[0].info);
        if (meta.hasWebsite && GUILayout.Button("Open website")) Process.Start(meta.metaInfo.Where(m => m.title == "website").ToArray()[0].info);
        if (meta.hasWikipedia && GUILayout.Button("Open wikipedia"))
        {
            string[] wiki = meta.metaInfo.Where(m => m.title == "wikipedia").ToArray()[0].info.Split(new[] {':'});
            string url = string.Format("https://{0}.wikipedia.org/wiki/{1}", wiki[0], wiki[1]);
            Process.Start(url);
        }

        EditorGUILayout.LabelField("Count: " + meta.metaInfo.Length);
        EditorGUILayout.Space();

        foreach (RealWorldTerrainMetaTag item in meta.metaInfo)
            EditorGUILayout.TextField(item.title, item.info);
    }
}