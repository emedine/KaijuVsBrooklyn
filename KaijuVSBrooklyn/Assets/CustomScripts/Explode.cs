using UnityEngine;
using System.Collections;

public class Explode : MonoBehaviour {

	// Use this for initialization
	void Start () {
        // Invoke("Explode");
        // doExplosion();

        var exp = GetComponent<ParticleSystem>();
        exp.Play();
        Destroy(gameObject, exp.duration);
    }
	
	// Update is called once per frame
	void Update () {
	
	}
    public void doExplosion()
    {
        var exp2 = GetComponent<ParticleSystem>();
        exp2.Play();
        Destroy(gameObject, exp2.duration);
    }
}
