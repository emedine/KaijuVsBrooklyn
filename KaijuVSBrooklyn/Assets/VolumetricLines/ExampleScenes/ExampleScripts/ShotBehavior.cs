using UnityEngine;
using System.Collections;

public class ShotBehavior : MonoBehaviour {

    public Transform m_shot;
    public GameObject m_explosion;

    // Use this for initialization
    void Start () {
        /// print("FIRE");
    }
	
	// Update is called once per frame
	void Update () {
		transform.position += transform.forward * Time.deltaTime * 700f;
	
	}
    void OnCollisionEnter(Collision col)
    {
        /// get the parent object of the collider and see what it's tag is
        /// if (col.gameObject.tag == "Destructable")
        if (col.gameObject.transform.parent.gameObject.tag == "Destructable")
        {
            ContactPoint contact = col.contacts[0];
            Vector3 pos = contact.point;
            //// print("Collision detected with trigger object " + col.gameObject.name);
            GameObject ego = GameObject.Instantiate(m_explosion, pos, col.gameObject.transform.rotation) as GameObject;
            Destroy(col.gameObject.transform.parent.gameObject);
            Debug.Log("YOU HIT SOMETHING");
            Object.Destroy(this.gameObject);
        }else
        {

            Debug.Log("pass thru");
        }
        
    }

    /*
    void OnCollisionEnter(Collision collision) {
        ContactPoint contact = collision.contacts[0];
        Quaternion rot = Quaternion.FromToRotation(Vector3.up, contact.normal);
        Vector3 pos = contact.point;
        Instantiate(explosionPrefab, pos, rot);
        Destroy(gameObject);
    }
    */
    /*
    void OnTriggerEnter(Collider other)
    {

        if(other.gameObject.transform.parent.tag == "Destructable")
        {
            print("Collision detected with trigger object " + other.name);
            /// throw new NotImplementedException();
            ///  GameObject ego = GameObject.Instantiate(m_explosion, m_shot.position, m_shot.rotation) as GameObject;
            GameObject ego = GameObject.Instantiate(m_explosion, other.transform.position, other.transform.rotation) as GameObject;
            Destroy(other.gameObject.transform.parent.gameObject);
            print("YOU HIT SOMETHING");
            Object.Destroy(this.gameObject);
        } else
        {

            Debug.Log("HIT NON DESTRUCTABLE");
        }
     
        if (other.gameObject.name == "ground" || other.gameObject.name == "Plane")
        {
            print("Collision detected with trigger object " + other.name);
           

        } else
        {
            print("Collision detected with trigger object " + other.name);
            /// throw new NotImplementedException();
            ///  GameObject ego = GameObject.Instantiate(m_explosion, m_shot.position, m_shot.rotation) as GameObject;
            GameObject ego = GameObject.Instantiate(m_explosion, other.transform.position, other.transform.rotation) as GameObject;
            Destroy(other.gameObject);
            print("YOU HIT SOMETHING");
            Object.Destroy(this.gameObject);
        }
       
    }
*/



}
