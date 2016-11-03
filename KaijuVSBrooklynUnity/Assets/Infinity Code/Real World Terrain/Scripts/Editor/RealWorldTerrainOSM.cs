/*     INFINITY CODE 2013-2015      */
/*   http://www.infinity-code.com   */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEditor;
using UnityEngine;

public static class RealWorldTerrainOSM
{
    public static string[] projectMaterials;

    public static MeshFilter AppendMesh(GameObject gameObject, Mesh mesh, Material material, string assetName)
    {
        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;
        meshFilter.mesh = mesh;

        MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
        renderer.materials = new[] {material};
        renderer.sharedMaterials = new[] {material};

        /*string osmFolder = Path.Combine(RealWorldTerrain.container.folder, "OSM");
        if (!Directory.Exists(osmFolder)) Directory.CreateDirectory(osmFolder);
        string assetPath = Path.Combine(osmFolder, assetName);
        AssetDatabase.CreateAsset(mesh, assetPath + ".asset");
        AssetDatabase.CreateAsset(material, assetPath + ".mat");*/

        return meshFilter;
    }

    public static void GenerateCompressedFile(byte[] data, ref List<RealWorldTerrainOSMNode> nodes, ref List<RealWorldTerrainOSMWay> ways, ref List<RealWorldTerrainOSMRelation> relations, string compressedFilename)
    {
        XmlDocument document = new XmlDocument();
        document.LoadXml(System.Text.Encoding.UTF8.GetString(data));

        if (nodes == null) nodes = new List<RealWorldTerrainOSMNode>();
        if (ways == null) ways = new List<RealWorldTerrainOSMWay>();
        if (relations == null) relations = new List<RealWorldTerrainOSMRelation>();

        if (document.DocumentElement == null) return;
        foreach (XmlNode node in document.DocumentElement.ChildNodes)
        {
            if (node.Name == "node") nodes.Add(new RealWorldTerrainOSMNode(node));
            else if (node.Name == "way") ways.Add(new RealWorldTerrainOSMWay(node));
            else if (node.Name == "relation") relations.Add(new RealWorldTerrainOSMRelation(node));
        }

        SaveOSM(compressedFilename, nodes, ways, relations);

        GC.Collect();
    }

    public static List<Vector3> GetGlobalPointsFromWay(RealWorldTerrainOSMWay way, List<RealWorldTerrainOSMNode> _nodes)
    {
        List<Vector3> points = new List<Vector3>();
        if (way.nodeRefs.Count == 0) return points;

        foreach (string nodeRef in way.nodeRefs)
        {
            RealWorldTerrainOSMNode node = _nodes.Find(n => n.id == nodeRef);
            if (node != null) points.Add(new Vector3(node.lon, 0, node.lat));
        }
        return points;
    }

    public static float GetTriangleDirectionOffset(Vector2 rp, ref Vector2 vp1, ref Vector2 vp2, float angle)
    {
        rp *= 100;
        if (vp1 == Vector2.zero)
        {
            vp1 = rp;
            return 0;
        }
        if (vp2 == Vector2.zero)
        {
            vp2 = vp1;
            vp1 = rp;
            return RealWorldTerrainUtils.Angle2D(vp2, vp1);
        }

        vp2 = vp1;
        vp1 = rp;
        float a = RealWorldTerrainUtils.Angle2D(vp2, vp1);
        if (a - angle > 180) a -= 360;
        else if (angle - a > 180) a += 360;
        return a - angle;
    }

    public static void LoadOSM(string _filename, out List<RealWorldTerrainOSMNode> _nodes, out List<RealWorldTerrainOSMWay> _ways, out List<RealWorldTerrainOSMRelation> _relations)
    {
        _nodes = new List<RealWorldTerrainOSMNode>();
        _relations = new List<RealWorldTerrainOSMRelation>();
        _ways = new List<RealWorldTerrainOSMWay>();

        if (!File.Exists(_filename)) return;

        FileStream fs = File.OpenRead(_filename);
        BinaryReader br = new BinaryReader(fs);

        int nodesCount = br.ReadInt32();

        for (int i = 0; i < nodesCount; i++) _nodes.Add(new RealWorldTerrainOSMNode(br));
        int wayCount = br.ReadInt32();
        for (int i = 0; i < wayCount; i++) _ways.Add(new RealWorldTerrainOSMWay(br));
        int relationCount = br.ReadInt32();
        for (int i = 0; i < relationCount; i++) _relations.Add(new RealWorldTerrainOSMRelation(br));
    }

    public static void SaveOSM(string _filename, List<RealWorldTerrainOSMNode> _nodes, List<RealWorldTerrainOSMWay> _ways, List<RealWorldTerrainOSMRelation> _relations)
    {
        FileStream fs = File.OpenWrite(_filename);
        BinaryWriter bw = new BinaryWriter(fs);

        if (_nodes != null)
        {
            _nodes = new List<RealWorldTerrainOSMNode>(_nodes.Distinct());
            bw.Write(_nodes.Count);
            foreach (RealWorldTerrainOSMNode node in _nodes) node.Write(bw);
        }
        else bw.Write(0);

        if (_ways != null)
        {
            _ways = new List<RealWorldTerrainOSMWay>(_ways.Distinct());
            bw.Write(_ways.Count);
            foreach (RealWorldTerrainOSMWay way in _ways) way.Write(bw);
        }
        else bw.Write(0);

        if (_relations != null)
        {
            _relations = new List<RealWorldTerrainOSMRelation>(_relations.Distinct());
            bw.Write(_relations.Count);
            foreach (RealWorldTerrainOSMRelation relation in _relations) relation.Write(bw);
        }
        else bw.Write(0);

        bw.Close();
    }
}