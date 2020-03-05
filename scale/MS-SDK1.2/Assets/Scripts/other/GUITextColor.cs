using UnityEngine;
using System.Collections;

public class GUITextColor : MonoBehaviour {
    public Color textColor;

	// Use this for initialization
	void Start () 
    {
        //guiText.material.color = textColor;
        guiText.text = "Use your hands and feet to interact. \n 用手和脚进行互动 \n KickAss 实验";
	}
	
}
