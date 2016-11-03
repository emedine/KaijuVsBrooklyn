/*     INFINITY CODE 2013-2015      */
/*   http://www.infinity-code.com   */

using System.Xml;
using UnityEngine;

public static class RealWorldTerrainXMLExt
{
    public static XmlNode CreateChild(this XmlNode node, string nodeName, bool value)
    {
        return node.CreateChild(nodeName, value.ToString());
    }

    public static XmlNode CreateChild(this XmlNode node, string nodeName, float value)
    {
        return node.CreateChild(nodeName, value.ToString());
    }

    public static XmlNode CreateChild(this XmlNode node, string nodeName, int value)
    {
        return node.CreateChild(nodeName, value.ToString());
    }

    public static XmlNode CreateChild(this XmlNode node, string nodeName, string value)
    {
        XmlDocument doc = node.OwnerDocument;
        if (doc == null) return null;
        XmlNode newNode = doc.CreateElement(nodeName);
        newNode.AppendChild(doc.CreateTextNode(value));
        node.AppendChild(newNode);
        return newNode;
    }

    public static XmlNode CreateChild(this XmlNode node, string nodeName, RealWorldTerrainRangeI value)
    {
        XmlNode newNode = node.CreateChild(nodeName);
        if (newNode == null) return null;
        newNode.CreateChild("Min", value.min);
        newNode.CreateChild("Max", value.max);
        return newNode;
    }

    public static XmlNode CreateChild(this XmlNode node, string nodeName, Vector2 value)
    {
        XmlNode newNode = node.CreateChild(nodeName);
        if (newNode == null) return null;
        newNode.CreateChild("X", value.x);
        newNode.CreateChild("Y", value.y);
        return newNode;
    }

    public static XmlNode CreateChild(this XmlNode node, string nodeName, RealWorldTerrainVector2i value)
    {
        XmlNode newNode = node.CreateChild(nodeName);
        if (newNode == null) return null;
        newNode.CreateChild("X", value.x);
        newNode.CreateChild("Y", value.y);
        return newNode;
    }

    public static XmlNode CreateChild(this XmlNode node, string nodeName, Vector3 value)
    {
        XmlNode newNode = node.CreateChild(nodeName);
        if (newNode == null) return null;
        newNode.CreateChild("X", value.x);
        newNode.CreateChild("Y", value.y);
        newNode.CreateChild("Z", value.z);
        return newNode;
    }

    public static XmlNode CreateChild(this XmlNode node, string nodeName)
    {
        XmlDocument doc = node.OwnerDocument;
        if (doc == null) return null;
        XmlNode newNode = doc.CreateElement(nodeName);
        node.AppendChild(newNode);
        return newNode;
    }

    public static float GetFloat(this XmlAttribute attr)
    {
        return float.Parse(attr.InnerText);
    }

    public static bool InnerBool(this XmlNode node)
    {
        return node.InnerText.ToLower() == "true";
    }

    public static float InnerFloat(this XmlNode node)
    {
        return float.Parse(node.InnerText);
    }

    public static int InnerInt(this XmlNode node)
    {
        return int.Parse(node.InnerText);
    }

    public static RealWorldTerrainRangeI InnerRangei(this XmlNode node)
    {
        int min = node.SelectSingleNode("Min").InnerInt();
        int max = node.SelectSingleNode("Max").InnerInt();
        return new RealWorldTerrainRangeI(min, max);
    }

    public static Vector2 InnerVector2(this XmlNode node)
    {
        float x = node.SelectSingleNode("X").InnerFloat();
        float y = node.SelectSingleNode("Y").InnerFloat();
        return new Vector2(x, y);
    }

    public static RealWorldTerrainVector2i InnerVector2i(this XmlNode node)
    {
        int x = node.SelectSingleNode("X").InnerInt();
        int y = node.SelectSingleNode("Y").InnerInt();
        return new RealWorldTerrainVector2i(x, y);
    }

    public static Vector3 InnerVector3(this XmlNode node)
    {
        float x = node.SelectSingleNode("X").InnerFloat();
        float y = node.SelectSingleNode("Y").InnerFloat();
        float z = node.SelectSingleNode("Z").InnerFloat();
        return new Vector3(x, y, z);
    }

    public static void SetAttribute(this XmlElement element, string name, float value)
    {
        element.SetAttribute(name, value.ToString());
    }

    public static void SetAttribute(this XmlElement element, string name, int value)
    {
        element.SetAttribute(name, value.ToString());
    }
}