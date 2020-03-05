using UnityEngine;
using System.Collections;

public class ClothesManager : MonoBehaviour {
    public enum selectMode { all, shirt, pant, bag };
    public selectMode currentMode = selectMode.pant;// reason of that currentmode is always "pant"   
    public Vector3 handPosition = Vector3.zero;
    public Renderer pantRenderer;
    public Renderer shirtRenderer;
    public Renderer bagRenderer;
    public int pantIndex, shirtIndex,bagIndex;

    public Texture2D[] pants;
    public Texture2D[] shirts;//include skirt
    public Texture2D[] bags;// modifiable, ex.decoration

    private Vector3 previousHandPos = Vector3.zero;
    private float lastSwipeTime = 0;
    Vector2 screenPosition = Vector2.zero;


    void Start()
    {
        UpdateGUIText();
    }
    public void HandPositionUpdate(Vector3 handPosition)  //捕捉到手的位置变化时，被外部脚本调用
    {
        Vector3 handScreenPos = GetJointInScreenPosition(handPosition); //获得转化的屏幕坐标
        screenPosition = handScreenPos;
        
        CheckForSwipeGesture(handPosition);
        
    }

   

    void CheckForSwipeGesture(Vector3 currentPosition)//gesture check
    {
        if (previousHandPos.x - currentPosition.x > 0.1f && Time.realtimeSinceStartup-lastSwipeTime>1) //向左挥
        {
            lastSwipeTime = Time.realtimeSinceStartup;
            ChangeElement(); //根据当前模式改变衣服
        }
        if (previousHandPos.y - currentPosition.y > 0.1f && Time.realtimeSinceStartup - lastSwipeTime > 1)//向下挥
        {
            lastSwipeTime = Time.realtimeSinceStartup;
            ChangeMode();
        }

        previousHandPos = currentPosition;
    }
    void ChangeElement()//change element
    {
        switch (currentMode)
        {
            case selectMode.shirt:
                shirtIndex++;
                if(shirtIndex>=shirts.Length) shirtIndex=0;// circle
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
        if (currentMode == selectMode.bag)
        {
            guiText.text = "?????:?";
            bagRenderer.enabled = true;
            shirtRenderer.enabled = false;
            pantRenderer.enabled = false;
        }
        if (currentMode == selectMode.pant)
        {
            guiText.text = "?????:??";
            bagRenderer.enabled = false;
            shirtRenderer.enabled = false;
            pantRenderer.enabled = true;
        }

        if (currentMode == selectMode.shirt)
        {
            guiText.text = "?????:??";
            bagRenderer.enabled = false;
            shirtRenderer.enabled = true;
            pantRenderer.enabled = false;
        }

        if (currentMode == selectMode.all)
        {
            guiText.text = "?????:??";
            bagRenderer.enabled = true;
            shirtRenderer.enabled = true;
            pantRenderer.enabled = true;
        }
    }
    Vector3 GetJointInScreenPosition(Vector3 jointPos)   //这个函数在 KinectWrapper.MapSkeletonPointToColorPoint() 的基础上加了屏幕分辨率自适应
    {
        Vector3 ScreenPos = KinectWrapper.MapSkeletonPointToColorPoint(jointPos);//kinectwapper package
        ScreenPos.x *= Screen.width / (float)KinectWrapper.GetDepthWidth();  //自动适应分辨率
        ScreenPos.y *= Screen.height / (float)KinectWrapper.GetDepthHeight();

        return ScreenPos;
    }
}
