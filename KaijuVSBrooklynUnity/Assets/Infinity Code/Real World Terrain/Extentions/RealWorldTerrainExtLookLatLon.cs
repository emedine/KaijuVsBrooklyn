/*     INFINITY CODE 2013-2015      */
/*   http://www.infinity-code.com   */

using System.Linq;
using UnityEngine;

public class RealWorldTerrainExtLookLatLon : MonoBehaviour
{
    public float distance = 10;
    public float height = 5;
    public float lat;
    public float lon;

    public static bool GetRealWorldPoint(out Vector3 position, float lon, float lat)
    {
        position = new Vector3();
        RealWorldTerrainContainer[] targets = (RealWorldTerrainContainer[]) FindObjectsOfType(typeof (RealWorldTerrainContainer));
        RealWorldTerrainContainer target = targets.FirstOrDefault(t =>
        {
            Debug.Log(t.area.ToString("F4"));
            return t.area.x <= lon && t.area.x + t.area.width >= lon && t.area.y >= lat &&
            t.area.y + t.area.height <= lat;
            ;
        });
                    
        if (target == null)
        {
            Debug.Log("Target not found");
            return false;
        }
        if (target.area.Contains(new Vector2(lon, lat)))
        {
            Debug.Log("Wrong coordinates");
            return false;
        }

        Terrain[] terrains = target.GetComponentsInChildren<Terrain>();
        if (terrains == null || terrains.Length == 0) return false;

        float minX = terrains.Min(t => t.gameObject.transform.position.x);
        float minZ = terrains.Min(t => t.gameObject.transform.position.z);
        float maxX = terrains.Max(t => t.gameObject.transform.position.x + t.terrainData.size.x);
        float maxZ = terrains.Max(t => t.gameObject.transform.position.z + t.terrainData.size.z);

        float lX = (lon - target.area.x) / target.area.width;
        float lZ = 1 - (lat - target.area.y) / target.area.height;
        if (lX < 0) lX = 0;
        if (lX > 1) lX = 1;
        if (lZ < 0) lZ = 0;
        if (lZ > 1) lZ = 1;

        float x = (maxX - minX) * lX + minX;
        float z = (maxZ - minZ) * lZ + minZ;

        Terrain terrain = terrains.FirstOrDefault(t => t.gameObject.transform.position.x <= x &&
                                                       t.gameObject.transform.position.z <= z &&
                                                       t.gameObject.transform.position.x + t.terrainData.size.x >= x &&
                                                       t.gameObject.transform.position.z + t.terrainData.size.z >= z);
        if (terrain == null) return false;

        float ix = (x - terrain.gameObject.transform.position.x) / terrain.terrainData.size.x;
        float iz = (z - terrain.gameObject.transform.position.z) / terrain.terrainData.size.z;
        float y = terrain.terrainData.GetInterpolatedHeight(ix, iz) + terrain.gameObject.transform.position.y;

        position = new Vector3(x, y, z);
        return true;
    }

    public static void LookTo(float lon, float lat)
    {
        Vector3 position;
        if (!GetRealWorldPoint(out position, lon, lat)) return;
        Camera.main.transform.LookAt(position);
    }

    public static void MoveTo(float lon, float lat, float distance, float height)
    {
        Vector3 position;
        if (!GetRealWorldPoint(out position, lon, lat)) return;
        Vector3 direction = Camera.main.transform.position - position;
        direction.y = 0;
        Vector3 newPosition = position + direction.normalized * distance;
        newPosition.y += height;
        Camera.main.transform.position = newPosition;
        Camera.main.transform.LookAt(position);
    }
}