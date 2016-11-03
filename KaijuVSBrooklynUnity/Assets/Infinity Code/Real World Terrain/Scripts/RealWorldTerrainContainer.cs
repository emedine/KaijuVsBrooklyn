/*     INFINITY CODE 2013-2015      */
/*   http://www.infinity-code.com   */

using UnityEngine;

[System.Serializable]
[AddComponentMenu("")]
public class RealWorldTerrainContainer : RealWorldTerrainMonoBase
{
    public string folder;
    public RealWorldTerrainVector2i terrainCount;
    public float detailDensity = 1;
    public float detailDistance = 80;

    private RealWorldTerrainItem[,] _terrains;
    public float treeDistance = 2000;
    public float billboardStart = 50;

    public RealWorldTerrainItem[,] terrains
    {
        get
        {
            if (_terrains == null)
            {
                _terrains = new RealWorldTerrainItem[terrainCount.x, terrainCount.y];
                RealWorldTerrainItem[] items = GetComponentsInChildren<RealWorldTerrainItem>();
                foreach (RealWorldTerrainItem item in items) _terrains[item.x, item.y] = item;
            }
            return _terrains;
        }
        set { _terrains = value; }
    }
}