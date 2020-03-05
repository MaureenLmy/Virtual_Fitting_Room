using UnityEngine;
using System.Collections;

public class HandsClapping : MonoBehaviour {
    public AvatarController avatar;
    public float previousDistance = 100;
    public Transform controlTarget;
	// Use this for initialization
	void Start () 
    {
        avatar = GetComponent<AvatarController>();   
	}
	
	// Update is called once per frame
	void Update ()
    {
        float currentDistance = Vector3.Distance(avatar.RightHand.position, avatar.LeftHand.position);
        if (currentDistance < 0.2f && previousDistance > 0.2f)
        {
            controlTarget.Rotate(Vector3.up, 30);
        }
        previousDistance = currentDistance;
	}
}
