using UnityEngine;
using System.Collections;
using System;

public class ARPhysicsManager : MonoBehaviour {

    #region vars
    //RGBImage 变量
    private IntPtr colorStreamHandle;// Image stream handles for the kinect
    private int imageWidth, imageHeight;
    private Color32[] colorImage;    // Color image data, if used
    private Texture2D colorMap;      //我们用来显示的彩色图

    //Skeleton 变量
    private KinectWrapper.NuiSkeletonFrame skeletonFrame;
    private KinectWrapper.NuiTransformSmoothParameters smoothParameters;
    public Vector3 rightHandScreenPos = Vector3.zero; //或缺手的屏幕坐标

    //Colliders 碰撞盒
    public Transform rightHandCollider;
    public Transform leftHandCollider;
    public Transform leftFootCollider;
    public Transform rightFootCollider;

    public Renderer RGBRenderer; //用来显示RGB图片的renderer
    #endregion vars

    void Start()//??kinect??????
    {
        #region 用KinectWrapper进行各种Nui的初始化
        int hr = 0; //用来存储各种函数返回值
        try
        {

            hr = KinectWrapper.NuiInitialize(KinectWrapper.NuiInitializeFlags.UsesDepthAndPlayerIndex |
                    KinectWrapper.NuiInitializeFlags.UsesSkeleton | KinectWrapper.NuiInitializeFlags.UsesColor);
            if (hr != 0)
            {
                throw new Exception("NuiInitialize Failed");
            }
            //public static extern int NuiImageStreamOpen(NuiImageType eImageType, NuiImageResolution eResolution, uint dwImageFrameFlags_NotUsed, uint dwFrameLimit, IntPtr hNextFrameEvent, ref IntPtr phStreamHandle);
            colorStreamHandle = IntPtr.Zero;
            hr = KinectWrapper.NuiImageStreamOpen(KinectWrapper.NuiImageType.Color, KinectWrapper.Constants.ImageResolution, 0, 2, IntPtr.Zero, ref colorStreamHandle);
            if (hr != 0)              //想直接利用KinectWrapper来得到彩色图，这里一直失败， 到底是哪里没有初始化好？
            {                         //后记：原因是没用先调用 KinectWrapper.NuiInitialize（）进行初始化
                throw new Exception("Cannot open color stream");
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
        #endregion 用KinectWrapper进行各种Nui的初始化

        imageWidth = KinectWrapper.GetDepthWidth();
        imageHeight = KinectWrapper.GetDepthHeight();
        int usersMapSize = imageWidth * imageHeight;
        colorImage = new Color32[usersMapSize];
        colorMap = new Texture2D(imageWidth, imageHeight);
        RGBRenderer.material.mainTexture = colorMap;


        // init skeleton structures
        skeletonFrame = new KinectWrapper.NuiSkeletonFrame();

    }

    // Update is called once per frame
    void Update()
    {
        #region Handle ColorFrame
        if (colorStreamHandle != IntPtr.Zero && KinectWrapper.PollColor(colorStreamHandle, ref colorImage))//第二个是你想用来接收刚更新好的颜色图的Color32数组
        {
            UpdateColorMap();
        }
        #endregion Handle ColorFrame

        #region Handle SkeletonFrame
        if (KinectWrapper.PollSkeleton(ref smoothParameters, ref skeletonFrame))
        {
            KinectWrapper.NuiSkeletonData skeleton = GetClosestSkeleton();

            if (skeleton.eTrackingState != KinectWrapper.NuiSkeletonTrackingState.NotTracked)
            {
                //根据需要在改变关机碰撞盒的位置
                SetColliderPosition(ref skeleton, KinectWrapper.NuiSkeletonPositionIndex.HandRight, ref rightHandCollider);
                SetColliderPosition(ref skeleton, KinectWrapper.NuiSkeletonPositionIndex.HandLeft, ref leftHandCollider);
                SetColliderPosition(ref skeleton, KinectWrapper.NuiSkeletonPositionIndex.FootRight, ref rightFootCollider);
                SetColliderPosition(ref skeleton, KinectWrapper.NuiSkeletonPositionIndex.FootLeft, ref leftFootCollider);
            }
        }
        #endregion HandleSkeleton Frame

        if (Input.GetKey(KeyCode.Escape)) Application.Quit();
    }

    // Update the Color Map
	void UpdateColorMap()//???????????????
    {
        colorMap.SetPixels32(colorImage);
        colorMap.Apply();     
    }


    void SetColliderPosition( ref KinectWrapper.NuiSkeletonData skeleton, KinectWrapper.NuiSkeletonPositionIndex index,
                              ref Transform jointCollider) //根据骨骼的位置来设置碰撞盒的位置
    {
        if (skeleton.eSkeletonPositionTrackingState[(int)index] == KinectWrapper.NuiSkeletonPositionTrackingState.Tracked)
        {
            Vector3 Pos = skeleton.SkeletonPositions[(int)index];
            Vector3 ScreenPos = KinectWrapper.MapSkeletonPointToColorPoint(Pos);
            //注意，这里返回的屏幕坐标是在，640x480分辨率下才对应得上的，其它分辨率在再乘上一个比例参数就可以了 
            ScreenPos.x *= Screen.width / (float)KinectWrapper.GetDepthWidth();
            ScreenPos.y *= Screen.height / (float)KinectWrapper.GetDepthHeight();

            Vector3 worldPos=Camera.main.ScreenToWorldPoint(new Vector3( ScreenPos.x,Screen.height-ScreenPos.y,RGBRenderer.transform.position.z)); //要用RGB图的Z值传进去才行
            jointCollider.position = new Vector3(worldPos.x, worldPos.y, jointCollider.position.z); //只改变x y 坐标值
        }
        
    }
   
    
    //简单得根据骨骼位置的z值返回最近的一个骨骼数据
    KinectWrapper.NuiSkeletonData GetClosestSkeleton()//?????????????
    {
        KinectWrapper.NuiSkeletonData skeletonData = new KinectWrapper.NuiSkeletonData();
        float minDistance = 8f;
        for (int i = 0; i < KinectWrapper.Constants.NuiSkeletonCount; i++)
        {
            KinectWrapper.NuiSkeletonData skeleton = skeletonFrame.SkeletonData[i];
            if (skeleton.eTrackingState != KinectWrapper.NuiSkeletonTrackingState.NotTracked)
            {
                if (skeleton.Position.z < minDistance)
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
