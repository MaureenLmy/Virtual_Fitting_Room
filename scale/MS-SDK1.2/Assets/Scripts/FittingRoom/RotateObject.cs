using UnityEngine;
using System.Collections;

public class RotateObject : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		transform.Rotate(new Vector3(2, 38, 19)*Time.deltaTime);//Rotate the object around its local axis---x=2 degree,y=38 degree,z=19 degree； vector3 三维向量
	}
}
