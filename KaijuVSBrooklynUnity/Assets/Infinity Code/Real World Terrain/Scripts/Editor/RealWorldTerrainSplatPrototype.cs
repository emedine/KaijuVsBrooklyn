/*     INFINITY CODE 2013-2015      */
/*   http://www.infinity-code.com   */

using System;
using System.Collections.Generic;
using System.Xml;
using UnityEditor;
using UnityEngine;

[Serializable]
public class RealWorldTerrainSplatPrototype
{
    public List<RealWorldTerrainGeneratorColor> colors;
    public bool deleted;
    private readonly bool isBase;
    private bool expanded = true;
    private Texture2D texture;
    private Vector2 tileOffset = Vector2.zero;
    private Vector2 tileSize = new Vector2(15, 15);

    public SplatPrototype splat
    {
        get
        {
            SplatPrototype sp = new SplatPrototype {texture = texture, tileSize = tileSize, tileOffset = tileOffset};
            return sp;
        }
    }

    public RealWorldTerrainSplatPrototype(bool isBase = false)
    {
        this.isBase = isBase;
        colors = new List<RealWorldTerrainGeneratorColor>();
    }

    public XmlNode GetNode(XmlDocument doc)
    {
        XmlElement node = doc.CreateElement("SplatPrototype");
        node.SetAttribute("tileSizeX", tileSize.x);
        node.SetAttribute("tileSizeY", tileSize.y);
        node.SetAttribute("tileOffsetX", tileOffset.x);
        node.SetAttribute("tileOffsetY", tileOffset.y);
        node.SetAttribute("textureID", (texture != null) ? texture.GetInstanceID() : -1);

        foreach (RealWorldTerrainGeneratorColor color in colors) node.AppendChild(color.GetNode(doc));

        return node;
    }

    public void OnGUI(int index = 0)
    {
        if (!isBase)
        {
            expanded = EditorGUILayout.Foldout(expanded, "SplatPrototype " + index);
            if (expanded)
            {
                OnGUIProp("Texture: ");
                int colorIndex = 1;
                foreach (RealWorldTerrainGeneratorColor color in colors) color.OnGUI(colorIndex++);
                colors.RemoveAll(c => c.deleted);

                GUILayout.BeginHorizontal();

                if (GUILayout.Button("Add color"))
                    colors.Add(new RealWorldTerrainGeneratorColor());

                if (GUILayout.Button("Generate preview"))
                    RealWorldTerrainSplatPrototypeGenerator.GeneratePreview(this);

                if (GUILayout.Button("Remove SplatPrototype"))
                    deleted = true;

                GUILayout.EndHorizontal();
            }
        }
        else
            OnGUIProp("Base texture: ");
    }

    private void OnGUIProp(string label)
    {
        texture = (Texture2D) EditorGUILayout.ObjectField(label, texture, typeof (Texture2D), false);
        tileSize = EditorGUILayout.Vector2Field("Tile size", tileSize);
        tileOffset = EditorGUILayout.Vector2Field("Tile offset", tileOffset);
    }

    public void SetNode(XmlElement node)
    {
        tileSize.x = float.Parse(node.GetAttribute("tileSizeX"));
        tileSize.y = float.Parse(node.GetAttribute("tileSizeY"));
        tileOffset.x = float.Parse(node.GetAttribute("tileOffsetX"));
        tileOffset.y = float.Parse(node.GetAttribute("tileOffsetY"));
        int textureID = int.Parse(node.GetAttribute("textureID"));
        if (textureID != -1) texture = (Texture2D) EditorUtility.InstanceIDToObject(textureID);
        else texture = null;

        colors = new List<RealWorldTerrainGeneratorColor>();

        foreach (XmlElement cNode in node.ChildNodes)
        {
            RealWorldTerrainGeneratorColor color = new RealWorldTerrainGeneratorColor();
            color.SetNode(cNode);
            colors.Add(color);
        }
    }
}