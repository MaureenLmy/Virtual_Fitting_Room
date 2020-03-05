using UnityEngine;
using System.Collections;
[RequireComponent (typeof(AvatarController))]
public class CrossHands : MonoBehaviour {
    private AvatarController avatar;

	// Use this for initialization
	void Start () 
    {
        avatar = GetComponent<AvatarController>();
	}
	
	// Update is called once per frame
	void Update () 
    {
        CheckCrossHands();
	}

    void CheckCrossHands()
    {
        if (avatar.LeftHand.position.y > avatar.LeftElbow.position.y && avatar.RightHand.position.y > avatar.RightElbow.position.y)
        {
            print("ddd");
        }
    }
}
