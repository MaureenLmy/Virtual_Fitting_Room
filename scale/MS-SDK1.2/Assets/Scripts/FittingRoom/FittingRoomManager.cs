using UnityEngine;
using System.Collections;
using System;

public class FittingRoomManager : MonoBehaviour
{
    #region vars
    //RGBImage 变量
    private IntPtr colorStreamHandle;// Image stream handles for the kinect
    private int imageWidth, imageHeight;
    private Color32[] colorImage;    // Color image data, if used
    private Texture2D colorMap;      //我们用来显示的彩色图

    //Skeleton 变量
    private KinectWrapper.NuiSkeletonFrame skeletonFrame;
    private KinectWrapper.NuiTransformSmoothParameters smoothParameters;
    

    public Renderer RGBRenderer;    //用来显示RGB图像的物体
    public Transform shirtTransform;
    public Transform shortPantTransform;
    public Transform bagTransform;


    //用来进行比例缩放的变量
    public Transform shoulderLeft;
    public Transform shoulderRight;
    private float shirt_originalDistance;
    private Vector3 shirt_originalScale;
	//++
	public Transform hipLeft;
	public Transform hipRight;
	private float shortpant_originalDistance;
	private Vector3 shortpant_originalScale;
	//++

    public ClothesManager clothesManger;

    #endregion vars

    void Start()
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
        
        //show the RGB image
        RGBRenderer.material.mainTexture = colorMap;

        // init skeleton structures
        skeletonFrame = new KinectWrapper.NuiSkeletonFrame();

        clothesManger = GetComponent<ClothesManager>();


        shirt_originalDistance = Vector3.Distance(shoulderLeft.position, shoulderRight.position); //初始的时候两个我们定的两个肩膀点的距离
        shirt_originalScale = shirtTransform.localScale;                                          //初始时候上衣面片的scale属性
        //++
		shortpant_originalDistance = Vector3.Distance(hipLeft.position, hipRight.position);///baocuo
		shortpant_originalScale = shortPantTransform.localScale;
		//++
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
                if (clothesManger != null) //交由另一个脚本来处理衣服的选择等等
                {
                    if (skeleton.eSkeletonPositionTrackingState[(int)KinectWrapper.NuiSkeletonPositionIndex.HandRight] != KinectWrapper.NuiSkeletonPositionTrackingState.NotTracked)
                    {
                        Vector3 rightHandPos = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.HandRight];
                        clothesManger.HandPositionUpdate(rightHandPos);
                    }
                }

                SetTShirtTransform(ref skeleton, ref shirtTransform);
                SetShortPantTransform(ref skeleton, ref shortPantTransform);
                SetBagTransform(ref skeleton, ref bagTransform);

                ResetShirtScale(ref skeleton);
				ResetPantScale(ref skeleton);//++ baocuo
            }
        }
        #endregion HandleSkeleton Frame

        if (Input.GetKey(KeyCode.Escape)) Application.Quit();
    }


    void SetColliderPosition(ref KinectWrapper.NuiSkeletonData skeleton, KinectWrapper.NuiSkeletonPositionIndex index,
                              ref Transform jointCollider) //根据骨骼的位置来设置碰撞盒的位置
    {
        if (skeleton.eSkeletonPositionTrackingState[(int)index] == KinectWrapper.NuiSkeletonPositionTrackingState.Tracked)
        {
            Vector3 Pos = skeleton.SkeletonPositions[(int)index];
            Vector3 ScreenPos = KinectWrapper.MapSkeletonPointToColorPoint(Pos);
            //注意，这里返回的屏幕坐标是在，640x480分辨率下才对应得上的，其它分辨率在再乘上一个比例参数就可以了 
            ScreenPos.x *= Screen.width / (float)KinectWrapper.GetDepthWidth();
            ScreenPos.y *= Screen.height / (float)KinectWrapper.GetDepthHeight();

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(ScreenPos.x, Screen.height - ScreenPos.y, RGBRenderer.transform.position.z)); //要用RGB图的Z值传进去才行
            jointCollider.position = new Vector3(worldPos.x, worldPos.y, jointCollider.position.z); //只改变x y 坐标值
        }

    }
    // Update the Color Map
    void UpdateColorMap()
    {
        colorMap.SetPixels32(colorImage);
        colorMap.Apply();    
    }

    

    #region 设置衣服面片位置和角度的各种函数； 由于衣服的不同部位所依据的关节不一样，所以要有不同的函数处理

    void SetTShirtTransform(ref KinectWrapper.NuiSkeletonData skeleton,ref Transform jointCollider) //根据骨骼的位置来设置碰撞盒的位置
    {
        if (skeleton.eSkeletonPositionTrackingState[(int)KinectWrapper.NuiSkeletonPositionIndex.Spine] != KinectWrapper.NuiSkeletonPositionTrackingState.NotTracked
            && skeleton.eSkeletonPositionTrackingState[(int)KinectWrapper.NuiSkeletonPositionIndex.ShoulderCenter] != KinectWrapper.NuiSkeletonPositionTrackingState.NotTracked)
        {
            //这里我们根据shoulderCenter来设置面片的位置， 再根据spine和shoulderCenter构成的矢量偏离垂直方向的夹角来设置面片的欧拉角
            Vector3 shouderCenterPos = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.ShoulderCenter];
            Vector3 SpinePos = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.Spine];
            Vector3 shoulderCenterScreenPos = GetJointInScreenPosition(shouderCenterPos);

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(shoulderCenterScreenPos.x, Screen.height - shoulderCenterScreenPos.y, RGBRenderer.transform.position.z)); //要用RGB图的Z值传进去才行
            jointCollider.position = new Vector3(worldPos.x, worldPos.y, jointCollider.position.z); //只改变x y 坐标值

            //上面是设置位置，下面是设置位置
            Vector3 upDirection = shouderCenterPos - SpinePos;
            jointCollider.up = upDirection; //同时设up 和right 的话会使得这个绕X轴的旋转无效，与绕Y轴旋转相比，目前只能先舍弃X轴

            Vector3 rightDirection = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.ShoulderRight] - skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.ShoulderLeft];
            jointCollider.right = rightDirection; 

            //要更好的效果要试着分别设欧拉角
        }

    }

    void SetShortPantTransform(ref KinectWrapper.NuiSkeletonData skeleton, ref Transform jointTransform)
    {
        if (skeleton.eSkeletonPositionTrackingState[(int)KinectWrapper.NuiSkeletonPositionIndex.HipCenter] != KinectWrapper.NuiSkeletonPositionTrackingState.NotTracked)
        {
            Vector3 hipCenterPos = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.HipCenter];
            Vector3 hipScreenPos = GetJointInScreenPosition(hipCenterPos);

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(hipScreenPos.x, Screen.height - hipScreenPos.y, RGBRenderer.transform.position.z)); //要用RGB图的Z值传进去才行
            jointTransform.position = new Vector3(worldPos.x, worldPos.y, jointTransform.position.z); //只改变x y 坐标值

            Vector3 rightDirection = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.HipRight] - skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.HipLeft];
            jointTransform.right = rightDirection; 

        }
    }


    void SetBagTransform(ref KinectWrapper.NuiSkeletonData skeleton, ref Transform jointTransform)
    {
        if (skeleton.eSkeletonPositionTrackingState[(int)KinectWrapper.NuiSkeletonPositionIndex.HandLeft] != KinectWrapper.NuiSkeletonPositionTrackingState.NotTracked)
        {
            Vector3 handLeftPos = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.HandLeft];
            Vector3 handLeftScreenPos = GetJointInScreenPosition(handLeftPos);

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(handLeftScreenPos.x, Screen.height - handLeftScreenPos.y, RGBRenderer.transform.position.z)); //要用RGB图的Z值传进去才行
            jointTransform.position = new Vector3(worldPos.x, worldPos.y, jointTransform.position.z); //只改变x y 坐标值

        }
    }
	//rescale
	//shirt
    void ResetShirtScale(ref KinectWrapper.NuiSkeletonData skeleton)
    {
        if (skeleton.eSkeletonPositionTrackingState[(int)KinectWrapper.NuiSkeletonPositionIndex.ShoulderLeft] != KinectWrapper.NuiSkeletonPositionTrackingState.NotTracked
           && skeleton.eSkeletonPositionTrackingState[(int)KinectWrapper.NuiSkeletonPositionIndex.ShoulderRight] != KinectWrapper.NuiSkeletonPositionTrackingState.NotTracked)
        {
            //这里我们根据shoulderCenter来设置面片的位置， 再根据spine和shoulderCenter构成的矢量偏离垂直方向的夹角来设置面片的欧拉角
            Vector3 shouderLeftPos = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.ShoulderLeft];
            Vector3 shoulderRightPos = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.ShoulderRight];
            Vector3 shoulderLeftScreenPos = GetJointInScreenPosition(shouderLeftPos);
            Vector3 shoulderRightScreenPos = GetJointInScreenPosition(shoulderRightPos);

            Vector3 shouderLWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(shoulderLeftScreenPos.x, Screen.height - shoulderLeftScreenPos.y, RGBRenderer.transform.position.z)); //要用RGB图的Z值传进去才行
            Vector3 shouderRWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(shoulderRightScreenPos.x, Screen.height - shoulderRightScreenPos.y, RGBRenderer.transform.position.z)); //要用RGB图的Z值传进去才行

            float curDistance = Vector3.Distance(shouderLWorldPos, shouderRWorldPos);
            float scaleFactor = curDistance / shirt_originalDistance;
            shirtTransform.localScale = shirt_originalScale * scaleFactor;
           
        }
    }
	//shortpant

	   void ResetPantScale(ref KinectWrapper.NuiSkeletonData skeleton)
    {
        if (skeleton.eSkeletonPositionTrackingState[(int)KinectWrapper.NuiSkeletonPositionIndex.HipLeft] != KinectWrapper.NuiSkeletonPositionTrackingState.NotTracked
           && skeleton.eSkeletonPositionTrackingState[(int)KinectWrapper.NuiSkeletonPositionIndex.HipRight] != KinectWrapper.NuiSkeletonPositionTrackingState.NotTracked)
        {
            //这里我们根据shoulderCenter来设置面片的位置， 再根据spine和shoulderCenter构成的矢量偏离垂直方向的夹角来设置面片的欧拉角
            Vector3 HipLeftPos = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.HipLeft];
            Vector3 HipRightPos = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.HipRight];
            Vector3 HipLeftScreenPos = GetJointInScreenPosition(HipLeftPos);
            Vector3 HipRightScreenPos = GetJointInScreenPosition(HipRightPos);

            Vector3 HipLWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(HipLeftScreenPos.x, Screen.height - HipLeftScreenPos.y, RGBRenderer.transform.position.z)); //要用RGB图的Z值传进去才行
            Vector3 HipRWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(HipRightScreenPos.x, Screen.height - HipRightScreenPos.y, RGBRenderer.transform.position.z)); //要用RGB图的Z值传进去才行

            float curDistance = Vector3.Distance(HipLWorldPos, HipRWorldPos);
			float scaleFactor = curDistance/shortpant_originalDistance;
			//shortPantTransform.localScale = shortpant_originalScale * scaleFactor;//baocuo
           
        }
    }

    
    #endregion 设置衣服面片函数

    #region 辅助函数
    Vector3 GetJointInScreenPosition(Vector3 jointPos)   //这个函数在 KinectWrapper.MapSkeletonPointToColorPoint() 的基础上加了屏幕分辨率自适应
    {   
        Vector3 ScreenPos = KinectWrapper.MapSkeletonPointToColorPoint(jointPos);
        ScreenPos.x *= Screen.width / (float)KinectWrapper.GetDepthWidth();  //自动适应分辨率
        ScreenPos.y *= Screen.height / (float)KinectWrapper.GetDepthHeight();

        return ScreenPos;
    }

    KinectWrapper.NuiSkeletonData GetClosestSkeleton()
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
    #endregion 辅助函数
   

    void OnApplicationQuit() //程序结束要做些收尾工作，不然Kinect不会自动关闭
    {
        KinectWrapper.NuiShutdown();
    }
}
