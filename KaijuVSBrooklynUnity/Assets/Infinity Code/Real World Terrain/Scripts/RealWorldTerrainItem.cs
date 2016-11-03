/*     INFINITY CODE 2013-2015      */
/*   http://www.infinity-code.com   */

using System;
using UnityEngine;

[System.Serializable]
[AddComponentMenu("")]
public class RealWorldTerrainItem : RealWorldTerrainMonoBase
{
    public RealWorldTerrainContainer container;
    public int x;
    public int y;

    public bool needUpdate = false;

    private Mesh _mesh;
    private Terrain _terrain;

    public Mesh mesh
    {
        get
        {
            if (_mesh == null) _mesh = GetComponent<MeshFilter>().sharedMesh;
            return _mesh;
        }
    }

    public Terrain terrain
    {
        get
        {
            if (_terrain == null) _terrain = GetComponent<Terrain>();
            return _terrain;
        }
    }

    public TerrainData terrainData
    {
        get
        {
            return terrain != null ? terrain.terrainData : null;
        }
    }

    public Bounds bounds
    {
        get
        {
            try
            {
                if (resultType == RealWorldTerrainResultType.terrain)
                {
                    Vector3 p1 = terrainData.size;
                    Vector3 p2 = transform.position + p1 / 2;
                    return new Bounds(p2, p1);
                }

                Bounds b;
                Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
                MeshFilter[] msf = GetComponentsInChildren<MeshFilter>();

                foreach (MeshFilter meshFilter in msf)
                {
                    b = meshFilter.sharedMesh.bounds;

                    if (min.x > b.min.x) min.x = b.min.x;
                    if (min.y > b.min.y) min.y = b.min.y;
                    if (min.z > b.min.z) min.z = b.min.z;

                    if (max.x < b.max.x) max.x = b.max.x;
                    if (max.y < b.max.y) max.y = b.max.y;
                    if (max.z < b.max.z) max.z = b.max.z;
                }

                Vector3 center = (min + max) / 2;
                Vector3 bsize = max - min;

                return new Bounds(Vector3.Scale(center, transform.localScale), Vector3.Scale(bsize, transform.localScale));
            }
            catch (Exception)
            {
                return new Bounds();
            }
            
        }
    }
}