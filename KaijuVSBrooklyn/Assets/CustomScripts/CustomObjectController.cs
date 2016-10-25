using UnityEngine;
using System.Collections;

public class CustomObjectController : MonoBehaviour {

    //// public float speed;
    public GameObject CamRig;
    

    private GameObject targObj;//  = this.gameObject;
    private Vector3 movement;
    public float speed = .125f;
    public float runSpeed = 5f;
    public float turnSmoothing = 15f;
    void Update()
    {
       ///  targObj = this.gameObject;
        float moveHorizontal = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).x;
        float moveVertical = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).y;
        /// transform.parent.rotation.SetEulerAngles(0, CamRig.transform.rotation.eulerAngles.y, 0);
        doMove(moveHorizontal, moveVertical);
        /*
        CamRig = GameObject.Find("OVRCameraRig");
        /// Debug.Log("mvt" + moveVertical);
        Vector3  movement = new Vector3(moveHorizontal * -1, 0.0f, moveVertical * -1);
        GetComponent<Rigidbody>().velocity = movement * speed;
        */

        // this.gameObject.transform.forward = CamRig.gameObject.transform.forward;


    }

    void doMove(float lh, float lv)
    {
                                                                              
         CamRig = GameObject.Find("CenterEyeAnchor");
        
        transform.rotation = Quaternion.Euler(0f, CamRig.transform.eulerAngles.y, 0f);
        /// transform.forward = CamRig.transform.forward;
        /// Vector3 newVec = new Vector3(CamRig.transform.forward.x * lh, CamRig.transform.forward.y * 0, CamRig.transform.forward.z * lv);
        transform.position = transform.position + (transform.right*lh) + (transform.forward * lv);
        /// Debug.Log(transform.localPosition);

          
        //movement.Set(lh, 0f, lv);
        //movement = Camera.main.transform.TransformDirection(movement);
        /// movement = CamRig.transform.TransformDirection(movement);
        // GetComponent<Rigidbody>().MovePosition(transform.position + movement);
        // this.transform.position = new Vector3(this.transform.position.x, 1, this.transform.position.z);
        /*
         movement.Set(lh, 0f, lv);

         ///CamRig = GameObject.Find("OVRCameraRig"); /// Camera.main.transform.

         ///movement = Camera.main.transform.TransformDirection(movement);

         GetComponent<Rigidbody>().MovePosition(transform.position + movement);
         movement.y = 0f;
         /// force object to not warp away from camera
         /// but kills the framerate

         ///this.transform.position = new Vector3(this.transform.position.x, 1, this.transform.position.z);

  
         

        if (lh != 0f || lv != 0f)
        {
            Rotating(lh, lv);
        }*/

    }

    void Rotating(float lh, float lv)
    {
        Vector3 targetDirection = new Vector3(lh, 0f, lv);

        Quaternion targetRotation = Quaternion.LookRotation(targetDirection, Vector3.up);

        Quaternion newRotation = Quaternion.Lerp(GetComponent<Rigidbody>().rotation, targetRotation, turnSmoothing * Time.deltaTime);

        GetComponent<Rigidbody>().MoveRotation(newRotation);
    }





}
