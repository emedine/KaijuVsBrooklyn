/*     INFINITY CODE 2013-2015      */
/*   http://www.infinity-code.com   */

using UnityEngine;

[AddComponentMenu("")]
public class RealWorldTerrainOSMBuilding : MonoBehaviour
{
    public float baseHeight;
    public Vector3[] baseVerticles;
    public RealWorldTerrainContainer container;
    public bool invertRoof;
    public bool invertWall;
    public MeshFilter roof;
    public float roofHeight;
    public RealWorldTerrainOSMRoofType roofType;
    public MeshFilter wall;
}