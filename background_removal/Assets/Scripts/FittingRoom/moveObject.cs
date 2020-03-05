using UnityEngine;
using System.Collections;

public class moveObject : MonoBehaviour {
   
    private Vector3 initPos;
    public float speed;
	// Use this for initialization
	void Start () 
    {
        initPos = transform.position;
        transform.position += Random.insideUnitSphere * 30;
        speed = Random.Range(1, 7);
	}
	
	// Update is called once per frame
	void Update ()
    {
        transform.Translate(Vector3.back * Time.deltaTime * speed,Space.World);
        if (transform.position.z < 0)
        {
            transform.position = initPos+ Random.insideUnitSphere * 30;
        }
	}
}
