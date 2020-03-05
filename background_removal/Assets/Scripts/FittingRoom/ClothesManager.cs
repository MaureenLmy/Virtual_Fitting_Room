using UnityEngine;
using System.Collections;

public class ClothesManager : MonoBehaviour {
	public enum selectMode { all, hat,scarp,shirt, pant, bag};
	public selectMode currentMode = selectMode.pant;
	public Vector3 handPosition = Vector3.zero;
	public Renderer pantRenderer;
	public Renderer shirtRenderer;
	public Renderer bagRenderer;
	public Renderer hatRenderer;
	public Renderer scarpRenderer;
	public int pantIndex, shirtIndex,bagIndex, hatIndex,scarpIndex;
	
	public Texture2D[] pants;
	public Texture2D[] shirts;
	public Texture2D[] bags;
	public Texture2D[] hats;
	public Texture2D[] scarps;
	
	private Vector3 previousHandPos = Vector3.zero;
	private float lastSwipeTime = 0;
	Vector2 screenPosition = Vector2.zero;
	
	
	void Start()
	{
		UpdateGUIText();
	}
	public void HandPositionUpdate(Vector3 handPosition)  //²¶×½µ½ÊÖµÄÎ»ÖÃ±ä»¯Ê±£¬±»Íâ²¿½Å±¾µ÷ÓÃ
	{
		Vector3 handScreenPos = GetJointInScreenPosition(handPosition); //»ñµÃ×ª»¯µÄÆÁÄ»×ø±ê
		screenPosition = handScreenPos;
		
		CheckForSwipeGesture(handPosition);
		
	}
	
	
	
	void CheckForSwipeGesture(Vector3 currentPosition)
	{
		if (previousHandPos.x - currentPosition.x > 0.1f && Time.realtimeSinceStartup-lastSwipeTime>1) //Ïò×ó»Ó
		{
			lastSwipeTime = Time.realtimeSinceStartup;
			ChangeElement(); //¸ù¾Ýµ±Ç°Ä£Ê½¸Ä±äÒÂ·þ
		}
		if (previousHandPos.y - currentPosition.y > 0.1f && Time.realtimeSinceStartup-lastSwipeTime>1) //Ïò×ó»Ó
		{
			lastSwipeTime = Time.realtimeSinceStartup;
			ChangeMode(); //¸ù¾Ýµ±Ç°Ä£Ê½¸Ä±äÒÂ·þ
		}
		/*if (currentPosition.y > (int)KinectWrapper.NuiSkeletonPositionIndex.ShoulderCenter 
		 && Time.realtimeSinceStartup - lastSwipeTime > 1)//ÏòÏÂ»Ó
        {
            lastSwipeTime = Time.realtimeSinceStartup;
            currentMode = selectMode.hat;
        }
		if (currentPosition.y < (int)KinectWrapper.NuiSkeletonPositionIndex.ShoulderCenter 
		&& currentPosition.y > (int)KinectWrapper.NuiSkeletonPositionIndex.HipCenter 
		&& Time.realtimeSinceStartup - lastSwipeTime > 1)//ÏòÏÂ»Ó
        {
            lastSwipeTime = Time.realtimeSinceStartup;
            currentMode = selectMode.shirt;
        }
		if (currentPosition.y < (int)KinectWrapper.NuiSkeletonPositionIndex.HipCenter 
		&& Time.realtimeSinceStartup - lastSwipeTime > 1)//ÏòÏÂ»Ó
		{
			lastSwipeTime = Time.realtimeSinceStartup;
			currentMode = selectMode.pant;
		}*/
		previousHandPos = currentPosition;
	}
	void ChangeElement()
	{
		switch (currentMode)
		{
		case selectMode.hat:
			hatIndex++;
			if (hatIndex >= hats.Length) hatIndex = 0;
			hatRenderer.material.mainTexture = hats[hatIndex];
			break;
		case selectMode.scarp:
			scarpIndex++;
			if (scarpIndex >= scarps.Length) scarpIndex = 0;
			scarpRenderer.material.mainTexture = scarps[scarpIndex];
			break;
		case selectMode.shirt:
			shirtIndex++;
			if(shirtIndex>=shirts.Length) shirtIndex=0;
			shirtRenderer.material.mainTexture = shirts[shirtIndex];
			break;
		case selectMode.pant:
			pantIndex++;
			if (pantIndex >= pants.Length) pantIndex = 0;
			pantRenderer.material.mainTexture = pants[pantIndex];
			break;
		case selectMode.bag:
			bagIndex++;
			if (bagIndex >= bags.Length) bagIndex = 0;
			bagRenderer.material.mainTexture = bags[bagIndex];
			break;
		}
		
	}
	void ChangeMode()
	{
		currentMode++;
		if (currentMode > selectMode.bag)
		{
			currentMode = 0;
		}
		UpdateGUIText();
	}
	
	void UpdateGUIText()
	{
		if (currentMode == selectMode.hat)
		{
			guiText.text = "hat ";
			hatRenderer.enabled = true;
			scarpRenderer.enabled  = false;
			bagRenderer.enabled = false;
			shirtRenderer.enabled = false;
			pantRenderer.enabled = false;
		}
		if (currentMode == selectMode.scarp)
		{
			guiText.text = "scarp ";
			hatRenderer.enabled = false;
			scarpRenderer.enabled  = true;
			bagRenderer.enabled = false;
			shirtRenderer.enabled = false;
			pantRenderer.enabled = false;
		}
		if (currentMode == selectMode.bag)
		{
			guiText.text = "bag ";
			scarpRenderer.enabled  = false;
			hatRenderer.enabled = false;
			bagRenderer.enabled = true;
			shirtRenderer.enabled = false;
			pantRenderer.enabled = false;
		}
		if (currentMode == selectMode.pant)
		{
			guiText.text = "pant ";
			scarpRenderer.enabled  = false;
			hatRenderer.enabled = false;
			bagRenderer.enabled = false;
			shirtRenderer.enabled = false;
			pantRenderer.enabled = true;
		}
		
		if (currentMode == selectMode.shirt)
		{
			guiText.text = "shirt";
			hatRenderer.enabled = false;
			scarpRenderer.enabled  = false;
			bagRenderer.enabled = false;
			shirtRenderer.enabled = true;
			pantRenderer.enabled = false;
		}
		
		if (currentMode == selectMode.all)
		{
			guiText.text = "all ";
			hatRenderer.enabled = true;
			scarpRenderer.enabled  = true;
			bagRenderer.enabled = true;
			shirtRenderer.enabled = true;
			pantRenderer.enabled = true;
		}
	}
	Vector3 GetJointInScreenPosition(Vector3 jointPos)   //Õâ¸öº¯ÊýÔÚ KinectWrapper.MapSkeletonPointToColorPoint() µÄ»ù´¡ÉÏ¼ÓÁËÆÁÄ»·Ö±æÂÊ×ÔÊÊÓ¦
	{
		Vector3 ScreenPos = KinectWrapper.MapSkeletonPointToColorPoint(jointPos);
		ScreenPos.x *= Screen.width / (float)KinectWrapper.GetDepthWidth();  //×Ô¶¯ÊÊÓ¦·Ö±æÂÊ
		ScreenPos.y *= Screen.height / (float)KinectWrapper.GetDepthHeight();
		
		return ScreenPos;
	}
}
