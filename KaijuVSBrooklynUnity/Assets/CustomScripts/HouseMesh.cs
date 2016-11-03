using UnityEngine;
using System.Collections;

public class HouseMesh : MonoBehaviour {

    private GameObject tChild;
    private GameObject gChild;

    //
    public Material[] randomMaterials;
    private Object[] facadeMatTall;/// aterials;
    private Object[] facadeMatWide;
    private Object[] facadeMatShort;
    // Use this for initialization
    void Start () {
        /// let's get some materials
        Object[] facadeMatWide = Resources.LoadAll("CustomWallMaterials/wide", typeof(Material));
        Object[] facadeMatTall = Resources.LoadAll("CustomWallMaterials/tall", typeof(Material));
        Object[] facadeMatShort = Resources.LoadAll("CustomWallMaterials/short", typeof(Material));
        /// Debug.Log("num mats: " + materials.Length);
        //// for loop to get all objects
        /// Debug.Log("Child Objects: " + CountChildren(transform));
        int children = transform.childCount;
        for (int i = 0; i < children; ++i)
        {
            /// print("house: " + transform.GetChild(i));
            tChild = transform.GetChild(i).gameObject;

            int gChildren = tChild.transform.childCount;
            for (int y = 0; y < gChildren; y++) {
                // col.gameObject.transform.parent.gameObject.tag == "Destructable")   
                gChild = tChild.transform.GetChild(y).gameObject;
                // print("grandchildren: " + gChild);

                ///////////// ADD ALL RANDOM MATERIALS /////////////////////////////////////////
                if(gChild.name == "Wall")
                {
                    Vector3 objectSize = Vector3.Scale(transform.localScale, gChild.GetComponent<MeshFilter>().mesh.bounds.size);
                    // if width of wall is bigger than height do wide
                    if(objectSize.x > objectSize.y){
                        gChild.GetComponent<Renderer>().material = (Material)facadeMatWide[Random.Range(0, facadeMatWide.Length)];

                    }
                    // if height of wall is bigger than width do tall
                    if (objectSize.x < objectSize.y * 2){
                        gChild.GetComponent<Renderer>().material = (Material)facadeMatTall[Random.Range(0, facadeMatTall.Length)];

                    }
                    if (objectSize.x < objectSize.y){
                        gChild.GetComponent<Renderer>().material = (Material)facadeMatShort[Random.Range(0, facadeMatShort.Length)];

                    }
                    // Debug.Log("Bounds: " + gChild.GetComponent<MeshFilter>().mesh.bounds);
                    // Debug.Log("Vec Bounds: " + objectSize);
                } else {
                    /// Debug.Log("error assigning material: " + gChild.name);

                }
               

                gChild.AddComponent<MeshCollider>();
                MeshCollider CCgb = gChild.GetComponent<MeshCollider>();
                CCgb.convex = true;
            }
           

        }


    }


    // Update is called once per frame
    void Update () {

    }


}
