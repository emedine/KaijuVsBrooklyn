/*     INFINITY CODE 2013-2015      */
/*   http://www.infinity-code.com   */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEngine;

public class RealWorldTerrainMonoBase : MonoBehaviour
{
    public Rect area;
    public int baseMapResolution;
    public int buildingGenerator = 0;
    public RealWorldTerrainRangeI buildingLevelLimits = new RealWorldTerrainRangeI(1, 50);
    public RealWorldTerrainBuildRCollider buildRCollider;
    public RealWorldTerrainBuildRRenderMode buildRRenderMode;
    public int customBuildRGeneratorStyle;
    public int customBuildRGeneratorTexturePack;
    public RealWorldTerrainBuildRPresetsItem[] customBuildRPresets;
    public float depthSharpness;
    public int detailResolution;
    public RealWorldTerrainElevationProvider elevationProvider;
    public bool generateTextures;
    public bool generatedBuildings;
    public bool generatedGrass;
    public bool generatedRivers;
    public bool generatedTextures;
    public bool generatedTrees;
    public int grassDensity = 100;
    public List<Texture2D> grassPrefabs;
    public int heightmapResolution;
    public float maxElevation;
    public int maxTextureLevel;
    public float minElevation;
    public short nodataValue;
    public List<RealWorldTerrainPOI> POI;
    public int resolutionPerPatch;
    public RealWorldTerrainResultType resultType;
    public string roadEngine;
    public Vector3 scale;
    public Vector3 size;
    public int sizeType;
    public Material splineBendMaterial;
    public Mesh splineBendMesh;
    public Vector3 terrainScale;
    public RealWorldTerrainVector2i textureCount;
    public RealWorldTerrainTextureProvider textureProvider;
    public string textureProviderURL;
    public RealWorldTerrainVector2i textureSize;
    public RealWorldTerrainTextureType textureType;
    public bool reduceTextures;
    public int treeDensity = 100;
    public List<GameObject> treePrefabs;
    public RealWorldTerrainOSMRoadType roadTypes;
    public List<RealWorldTerrainBuildingMaterial> buildingMaterials;

    public Vector2 bottomRight
    {
        get { return new Vector2(area.x + area.width, area.y + area.height); }
    }

    public Vector2 topLeft
    {
        get { return new Vector2(area.x, area.y); }
    }
}

[Serializable]
public class RealWorldTerrainBuildRPresetsItem
{
    public string facade;
    public string roof;
    public string texture;
}

public class RealWorldTerrainOSMBase
{
    public string id;
    public List<RealWorldTerrainOSMTag> tags;

    public bool Equals(RealWorldTerrainOSMBase other)
    {
        if (ReferenceEquals(other, null)) return false;
        if (ReferenceEquals(this, other)) return true;
        return id == other.id;
    }

    public override int GetHashCode()
    {
        return id.GetHashCode();
    }

    public string GetTagValue(string key)
    {
        List<RealWorldTerrainOSMTag> curTags = tags.Where(tag => tag.key == key).ToList();
        if (curTags.Count > 0) return curTags[0].value;
        return string.Empty;
    }

    public bool HasTag(string key, string value)
    {
        return tags.Any(t => t.key == key && t.value == value);
    }

    public bool HasTagKey(params string[] keys)
    {
        return keys.Any(key => tags.Any(t => t.key == key));
    }

    public bool HasTagValue(params string[] values)
    {
        return values.Any(val => tags.Any(t => t.value == val));
    }

    public bool HasTags(string key, params string[] values)
    {
        return tags.Any(tag => tag.key == key && values.Any(v => v == tag.value));
    }
}

public class RealWorldTerrainOSMNode : RealWorldTerrainOSMBase
{
    public readonly float lat;
    public readonly float lon;

    public RealWorldTerrainOSMNode(BinaryReader br)
    {
        id = br.ReadInt64().ToString();
        lat = br.ReadSingle();
        lon = br.ReadSingle();

        tags = new List<RealWorldTerrainOSMTag>();
        int tagCount = br.ReadInt32();
        for (int i = 0; i < tagCount; i++) tags.Add(new RealWorldTerrainOSMTag(br));
    }

    public RealWorldTerrainOSMNode(XmlNode node)
    {
        id = node.Attributes["id"].Value;
        lat = float.Parse(node.Attributes["lat"].Value);
        lon = float.Parse(node.Attributes["lon"].Value);

        tags = new List<RealWorldTerrainOSMTag>();

        foreach (XmlNode subNode in node.ChildNodes) tags.Add(new RealWorldTerrainOSMTag(subNode));
    }

    public void Write(BinaryWriter bw)
    {
        bw.Write(long.Parse(id));
        bw.Write(lat);
        bw.Write(lon);
        bw.Write(tags.Count);
        foreach (RealWorldTerrainOSMTag tag in tags) tag.Write(bw);
    }
}

public class RealWorldTerrainOSMRelation : RealWorldTerrainOSMBase
{
    public readonly List<RealWorldTerrainOSMRelationMember> members;

    public RealWorldTerrainOSMRelation(BinaryReader br)
    {
        id = br.ReadInt64().ToString();
        members = new List<RealWorldTerrainOSMRelationMember>();
        tags = new List<RealWorldTerrainOSMTag>();

        int memberCount = br.ReadInt32();
        for (int i = 0; i < memberCount; i++) members.Add(new RealWorldTerrainOSMRelationMember(br));
        int tagCount = br.ReadInt32();
        for (int i = 0; i < tagCount; i++) tags.Add(new RealWorldTerrainOSMTag(br));
    }

    public RealWorldTerrainOSMRelation(XmlNode node)
    {
        id = node.Attributes["id"].Value;
        members = new List<RealWorldTerrainOSMRelationMember>();
        tags = new List<RealWorldTerrainOSMTag>();

        foreach (XmlNode subNode in node.ChildNodes)
        {
            if (subNode.Name == "member") members.Add(new RealWorldTerrainOSMRelationMember(subNode));
            else if (subNode.Name == "tag") tags.Add(new RealWorldTerrainOSMTag(subNode));
        }
    }

    public void Write(BinaryWriter bw)
    {
        bw.Write(long.Parse(id));
        bw.Write(members.Count);
        foreach (RealWorldTerrainOSMRelationMember member in members) member.Write(bw);
        bw.Write(tags.Count);
        foreach (RealWorldTerrainOSMTag tag in tags) tag.Write(bw);
    }
}

public class RealWorldTerrainOSMRelationMember
{
    public readonly string reference;

    private readonly string role;
    private readonly string type;

    public RealWorldTerrainOSMRelationMember(BinaryReader br)
    {
        type = br.ReadString();
        reference = br.ReadInt64().ToString();
        role = br.ReadString();
    }

    public RealWorldTerrainOSMRelationMember(XmlNode node)
    {
        type = node.Attributes["type"].Value;
        reference = node.Attributes["ref"].Value;
        role = node.Attributes["role"].Value;
    }

    public void Write(BinaryWriter bw)
    {
        bw.Write(type);
        bw.Write(long.Parse(reference));
        bw.Write(role);
    }
}

public class RealWorldTerrainOSMTag
{
    public readonly string key;
    public readonly string value;

    public RealWorldTerrainOSMTag(BinaryReader br)
    {
        key = br.ReadString();
        value = br.ReadString();
    }

    public RealWorldTerrainOSMTag(XmlNode node)
    {
        key = node.Attributes["k"].Value;
        value = node.Attributes["v"].Value;
    }

    public void Write(BinaryWriter bw)
    {
        bw.Write(key);
        bw.Write(value);
    }
}

public class RealWorldTerrainOSMWay : RealWorldTerrainOSMBase
{
    public readonly List<string> nodeRefs;

    public RealWorldTerrainOSMWay(BinaryReader br)
    {
        id = br.ReadInt64().ToString();
        nodeRefs = new List<string>();
        tags = new List<RealWorldTerrainOSMTag>();
        int refCount = br.ReadInt32();
        for (int i = 0; i < refCount; i++) nodeRefs.Add(br.ReadInt64().ToString());
        int tagCount = br.ReadInt32();
        for (int i = 0; i < tagCount; i++) tags.Add(new RealWorldTerrainOSMTag(br));
    }

    public RealWorldTerrainOSMWay(XmlNode node)
    {
        id = node.Attributes["id"].Value;
        nodeRefs = new List<string>();
        tags = new List<RealWorldTerrainOSMTag>();

        foreach (XmlNode subNode in node.ChildNodes)
        {
            if (subNode.Name == "nd") nodeRefs.Add(subNode.Attributes["ref"].Value);
            else if (subNode.Name == "tag") tags.Add(new RealWorldTerrainOSMTag(subNode));
        }
    }

    public void Write(BinaryWriter bw)
    {
        bw.Write(long.Parse(id));
        bw.Write(nodeRefs.Count);
        foreach (string nodeRef in nodeRefs) bw.Write(long.Parse(nodeRef));
        bw.Write(tags.Count);
        foreach (RealWorldTerrainOSMTag tag in tags) tag.Write(bw);
    }
}

public class RealWorldTerrainPOI
{
    public string title;
    public float x;
    public float y;

    public RealWorldTerrainPOI(string title, float x, float y)
    {
        this.title = title;
        this.x = x;
        this.y = y;
    }

    public RealWorldTerrainPOI(XmlNode node)
    {
        try
        {
            x = node.Attributes["x"].GetFloat();
            y = node.Attributes["y"].GetFloat();
            title = node.InnerText;
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
            throw;
        }
    }
}

[Serializable]
public class RealWorldTerrainRangeI
{
    public int min = 1;
    public int minLimit = int.MinValue;
    public int max = 50;
    public int maxLimit = int.MaxValue;

    public RealWorldTerrainRangeI(int min, int max, int minLimit = int.MinValue, int maxLimit = int.MaxValue)
    {
        this.min = min;
        this.max = max;
        this.minLimit = minLimit;
        this.maxLimit = maxLimit;
    }

    public void Set(float min, float max)
    {
        this.min = Mathf.Max(minLimit, (int)min);
        this.max = Mathf.Min(maxLimit, (int)max);
    }

    public int Random()
    {
        return UnityEngine.Random.Range(min, max);
    }
}

[Serializable]
public class RealWorldTerrainBuildingMaterial
{
    public Material wall;
    public Material roof;
}