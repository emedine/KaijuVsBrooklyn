using UnityEngine;
using System.Collections;
using System;

public class EyeballLaserBehavior : MonoBehaviour
{
    /*
    public Transform m_cannonRot;
    public Transform m_muzzle;
    public Transform m_eyeRot;
    */
    public Transform m_eyeball;
    public GameObject m_shotPrefab;
    public Texture2D m_guiTexture;


    /// movement controls
    private Vector3 moveDirection = Vector3.zero;
    public float speed = 6.0F;
    public float jumpSpeed = 8.0F;
    public float gravity = 20.0F;


    // Use this for initialization
    void Start()
    {
        /// OVRTouchpad.Create();
        OVRTouchpad.TouchHandler += HandleTouchHandler;

    }

    private void HandleTouchHandler(object sender, EventArgs e)
    {
        /// throw new NotImplementedException();
        GameObject ego = GameObject.Instantiate(m_shotPrefab, m_eyeball.position, m_eyeball.rotation) as GameObject;
        GameObject.Destroy(ego, 3f);


        //// check for back button

   

    }

    // Update is called once per frame
    void Update() {
        /*
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            m_cannonRot.transform.Rotate(Vector3.up, -Time.deltaTime * 100f);
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            m_cannonRot.transform.Rotate(Vector3.up, Time.deltaTime * 100f);
        }
        */
        /*if (Input.GetButtonDown("Back"))
        {
            Debug.Log("Clicked Back Button.");
            //ShowGlobalMenu();
        } */ // end "Back"
        if (Input.GetKeyDown(KeyCode.Space) || OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger))
        {
           /// GameObject go = GameObject.Instantiate(m_shotPrefab, m_muzzle.position, m_muzzle.rotation) as GameObject;
           /// GameObject.Destroy(go, 3f);

            GameObject ego = GameObject.Instantiate(m_shotPrefab, m_eyeball.position, m_eyeball.rotation) as GameObject;
            GameObject.Destroy(ego, 3f);
        }

       

        ////TargetPosition
    }




    void OnGUI()
    {
        /// GUI.DrawTexture(new Rect(0f, 0f, m_guiTexture.width / 2, m_guiTexture.height / 2), m_guiTexture);
    }
}
