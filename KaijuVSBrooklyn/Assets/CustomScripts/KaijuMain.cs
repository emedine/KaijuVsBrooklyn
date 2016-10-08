using UnityEngine;
using System.Collections;

public class KaijuMain : MonoBehaviour {

    public float speed = 1.5f;

    public bool hasTarget = true;
    private Vector3 targPos;
    private Vector3 camPos;
    private GameObject gObj;
    private GameObject CamRig;
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        //transform.position = Input.mousePosition;
        // Debug.Log("tp:" + transform.position);
        // Debug.Log("mp:" + Input.mousePosition);
        CamRig = GameObject.Find("OVRCameraRig");
        
        if (hasTarget == false) {
            gObj = GameObject.Find("TargetPosMarker");
            if (gObj) {
                Vector3 tmpPos = Vector3.Lerp(transform.position, gObj.transform.position, speed / 23);
                /// CamRig.transform.position = Vector3.Lerp(transform.position, gObj.transform.position, speed / 150);

                CamRig.transform.position = new Vector3(tmpPos.x, 10, tmpPos.z);
            }
        }

        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger)) {
            /// Debug.Log("Primary Trigger HIT");
            hasTarget = false;
            /// update position of target
            var gObj = GameObject.Find("TargetPosMarker");
            if (gObj) {
                targPos = new Vector3(gObj.transform.position.x, 10 , gObj.transform.position.z);
                CamRig = GameObject.Find("OVRCameraRig");
                // CamRig.transform.position = gObj.transform.position;
                // CamRig.transform.Translate(0, 10, 0); /// make sure height doesn't change 
                Debug.Log("New x: " + CamRig.transform.position.x + "New Y: " + CamRig.transform.position.y + "New Z: " + CamRig.transform.position.z + " targ X: " + targPos.x + " targY: " + targPos.y + " targZ: " + targPos.z);
                // Debug.Log("New x: " + this.gameObject.transform.position.x + "New Y: " + this.gameObject.transform.position.x + " targ X: " + newPos.x + " targY: " + newPos.y);
            }
            else
            {
                Debug.Log("NO MARKER FOUND");

            }


        }

    }

    /////// check to see if we're hitting the target, if so stop moving
    /*
    void OnCollisionEnter(Collision col)
    {
        /// get the parent object of the collider and see what it's tag is
        
        
        if (col.gameObject.name == "TargetPosMarker")
        {
            /// Destroy(col.gameObject);
            print("TARGET REACHED " + col.gameObject.name);
            hasTarget = true;

            /// GameObject ego = GameObject.Instantiate(m_explosion, col.gameObject.transform.position, col.gameObject.transform.rotation) as GameObject;
            ///Destroy(col.gameObject.transform.parent.gameObject);
            ///Debug.Log("YOU HIT SOMETHING");
            /// Object.Destroy(this.gameObject);
        }
        else
        {

            Debug.Log("pass thru" + col.gameObject.name);
        }

    }
    */

    void OnTriggerEnter(Collider other)
    {

        if (other.gameObject.name == "TargetPosMarker")
        {
            /// Destroy(col.gameObject);
            print("TARGET REACHED " + other.gameObject.name);
            hasTarget = true;

            /// GameObject ego = GameObject.Instantiate(m_explosion, col.gameObject.transform.position, col.gameObject.transform.rotation) as GameObject;
            ///Destroy(col.gameObject.transform.parent.gameObject);
            ///Debug.Log("YOU HIT SOMETHING");
            /// Object.Destroy(this.gameObject);
        }
        else
        {

            Debug.Log("pass thru" + other.gameObject.name);
        }

    }

}
