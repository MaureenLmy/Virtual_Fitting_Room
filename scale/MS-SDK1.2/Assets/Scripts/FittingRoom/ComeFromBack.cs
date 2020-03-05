using UnityEngine;
using System.Collections;

public class ComeFromBack : MonoBehaviour {
    public GameObject prefab;
	// Use this for initialization
	void Start () {
        for (int i = 0; i < 6; i++)
        {
            GameObject go=Instantiate(prefab) as GameObject;
            go.transform.localScale = Vector3.one * 5;//x:1 y:1 z:1 degree 累加5
        }
	}

    // Update is called once per frame
    void Update()
    {
	
	}
}
