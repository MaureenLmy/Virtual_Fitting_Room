using UnityEngine;
using System.Collections;
using System;

public class SkeletonBasic : MonoBehaviour {
    // Skeleton related structures
    private KinectWrapper.NuiSkeletonFrame skeletonFrame;
    private KinectWrapper.NuiTransformSmoothParameters smoothParameters;
    public GUIText textTip;
    public GUIText skeletonToDepth;
    public Transform rightHand;  //用来跟随右手骨骼的物体

    void Start()
    {
        #region 初始化Nui和骨骼流
        int hr = 0;
        try
        {
            hr = KinectWrapper.NuiInitialize(KinectWrapper.NuiInitializeFlags.UsesDepthAndPlayerIndex |
                KinectWrapper.NuiInitializeFlags.UsesSkeleton | KinectWrapper.NuiInitializeFlags.UsesColor);
            if (hr != 0)
            {
                throw new Exception("NuiInitialize Failed");
            }

            hr = KinectWrapper.NuiSkeletonTrackingEnable(IntPtr.Zero, 8);  // 0, 12,8
            if (hr != 0)
            {
                throw new Exception("Cannot initialize Skeleton Data");
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message + " - " + KinectWrapper.GetNuiErrorString(hr));
            return;
        }
        #endregion

        // init skeleton structures
        skeletonFrame = new KinectWrapper.NuiSkeletonFrame();
        textTip = GetComponent<GUIText>();
       
    }
	
	// Update is called once per frame
	void Update () 
    {
        if (KinectWrapper.PollSkeleton(ref smoothParameters, ref skeletonFrame))
        {
            KinectWrapper.NuiSkeletonData skeleton = GetClosestSkeleton();

            if (skeleton.eTrackingState == KinectWrapper.NuiSkeletonTrackingState.NotTracked)
            {
                textTip.text = "No skeleton tracked";
            }
            else
            {
                textTip.text = "UserID: " + skeleton.dwTrackingID + "\nUserIndex: " + skeleton.dwUserIndex
                    +"\nQuality: "+skeleton.dwQualityFlags;//这里只是简单的返回位运算符，要想判断哪个边切到了，可以这样bool clippedBottom = (skeleton.dwQualityFlags & (int)KinectWrapper.FrameEdges.Bottom) != 0;

                
                if (skeleton.eSkeletonPositionTrackingState[(int)KinectWrapper.NuiSkeletonPositionIndex.HandRight]==KinectWrapper.NuiSkeletonPositionTrackingState.Tracked)
                {     
                    //能追踪到右手就把用模型对应一下右手的位置
                    rightHand.position = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.HandRight];
                    //rightHand.rotation=               //貌似没有获取骨骼rotation属性的函数，怎么弄？

                    Vector3 depthPoint = KinectWrapper.MapSkeletonPointToDepthPoint(rightHand.position);
                    Vector3 colorPoint = KinectWrapper.MapSkeletonPointToColorPoint(rightHand.position);
                    skeletonToDepth.text = "DepthPoint:   X: " + depthPoint.x + " Y :" + depthPoint.y + " Z" + depthPoint.z+"\n"+
                                           "ColorPoint:   X: " + colorPoint.x + " Y :" + colorPoint.y + " Z" +colorPoint.z ;
                }
            }
        }
	}
   
    //简单得根据骨骼位置的z值返回最近的一个骨骼数据
    KinectWrapper.NuiSkeletonData GetClosestSkeleton()
    {
        KinectWrapper.NuiSkeletonData skeletonData=new KinectWrapper.NuiSkeletonData();
        float minDistance = 8f;
        for (int i = 0; i < KinectWrapper.Constants.NuiSkeletonCount; i++)
        {
            KinectWrapper.NuiSkeletonData skeleton = skeletonFrame.SkeletonData[i];
            if (skeleton.eTrackingState != KinectWrapper.NuiSkeletonTrackingState.NotTracked)
            {
                if (skeleton.Position.z< minDistance)
                {
                    skeletonData = skeleton;
                    minDistance = skeleton.Position.z;
                }
            }
        }

        return skeletonData;
    }
    void OnApplicationQuit() //程序结束要做些收尾工作，不然Kinect不会自动关闭
    {
        KinectWrapper.NuiShutdown();
    }
}
