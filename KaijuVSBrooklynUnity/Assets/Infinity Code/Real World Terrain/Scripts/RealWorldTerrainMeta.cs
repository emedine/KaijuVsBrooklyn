/*     INFINITY CODE 2013-2015      */
/*   http://www.infinity-code.com   */

using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("")]
public class RealWorldTerrainMeta : MonoBehaviour
{
    public Vector2 center;
    public bool hasURL;
    public bool hasWebsite;
    public bool hasWikipedia;
    public RealWorldTerrainMetaTag[] metaInfo;

    private void AddInfo(string title, string info)
    {
        if (metaInfo == null) metaInfo = new RealWorldTerrainMetaTag[0];

        List<RealWorldTerrainMetaTag> metaList = new List<RealWorldTerrainMetaTag>(metaInfo)
        {
            new RealWorldTerrainMetaTag {info = info, title = title}
        };

        if (title == "url") hasURL = true;
        else if (title == "website") hasWebsite = true;
        else if (title == "wikipedia") hasWikipedia = true;

        metaInfo = metaList.ToArray();
    }

    public RealWorldTerrainMeta GetFromOSM(RealWorldTerrainOSMBase item, Vector2 center = default(Vector2))
    {
        foreach (RealWorldTerrainOSMTag itemTag in item.tags) AddInfo(itemTag.key, itemTag.value);
        this.center = center;

        return this;
    }
}

[System.Serializable]
public class RealWorldTerrainMetaTag
{
    public string info;
    public string title;
}