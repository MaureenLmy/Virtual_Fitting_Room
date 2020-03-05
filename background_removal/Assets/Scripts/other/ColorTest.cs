using UnityEngine;
using System.Collections;

public class ColorTest : MonoBehaviour {
    public float r, g, b;

	
	// Update is called once per frame
	void Update () {
        renderer.material.color = new Color(r, g, b);
	}
}
