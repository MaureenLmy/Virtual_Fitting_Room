using UnityEngine;
using System.Collections;

public class background : MonoBehaviour {
	public enum selectMode { video, picture };
	public selectMode currentMode = selectMode.picture;
	
	public Vector3 handPosition = Vector3.zero;
	public Renderer videoRenderer;
	public Renderer pictureRenderer;
	
	public int videoIndex, pictureIndex;
	
	public Texture2D[] video;
	public Texture2D[] picture;
	
	private Vector3 previousHandPos = Vector3.zero;
	private float lastSwipeTime = 0;
	Vector2 screenPosition = Vector2.zero;
	
	
	// Use this for initialization
	void Start () {
		UpdateGUIText();
	}
	
	public void HandPositionUpdate(Vector3 handPosition)  
	{
		Vector3 handScreenPos = GetJointInScreenPosition(handPosition); 
		screenPosition = handScreenPos;
		
		CheckForSwipeGesture(handPosition);
		
	}
	
	void CheckForSwipeGesture(Vector3 currentPosition)
	{
		if (previousHandPos.x - currentPosition.x > 0.1f && Time.realtimeSinceStartup-lastSwipeTime>1) //?ò×ó?ó
		{
			lastSwipeTime = Time.realtimeSinceStartup;
			ChangeElement(); //?ù?Yμ±?°?￡ê???±?ò?·t
		}
		if (previousHandPos.y - currentPosition.y > 0.1f && Time.realtimeSinceStartup - lastSwipeTime > 1)//?ò???ó
		{
			lastSwipeTime = Time.realtimeSinceStartup;
			ChangeMode();
		}
		
		previousHandPos = currentPosition;
	}
	
	void ChangeElement()
	{
		switch (currentMode) {
		case selectMode.picture:
			pictureIndex++;
			if (pictureIndex >= picture.Length)
				pictureIndex = 0;
			pictureRenderer.material.mainTexture = picture [pictureIndex];
			break;
		case selectMode.video:
			videoIndex++;
			if (videoIndex >= video.Length)
				videoIndex = 0;
			videoRenderer.material.mainTexture = video [videoIndex];
			break;
		}
	}
	void ChangeMode()
	{
		currentMode++;
		if (currentMode > selectMode.picture)
		{
			currentMode = 0;
		}
		UpdateGUIText();
	}
	
	void UpdateGUIText()
	{
		if (currentMode == selectMode.picture) {
			pictureRenderer.enabled = true;
			videoRenderer.enabled = false;
		}
		if (currentMode == selectMode.video) {
			pictureRenderer.enabled = false;
			videoRenderer.enabled = false;
		}
	}
	
	// Update is called once per frame
	void Update () {
		
	}
	Vector3 GetJointInScreenPosition(Vector3 jointPos)   //?a??oˉêy?ú KinectWrapper.MapSkeletonPointToColorPoint() μ??ù′?é??óá??á??·?±??ê×?êêó|
	{
		Vector3 ScreenPos = KinectWrapper.MapSkeletonPointToColorPoint(jointPos);
		ScreenPos.x *= Screen.width / (float)KinectWrapper.GetDepthWidth();  //×??ˉêêó|·?±??ê
		ScreenPos.y *= Screen.height / (float)KinectWrapper.GetDepthHeight();
		
		return ScreenPos;
	}
}
