using UnityEngine;
using System.Collections;
using System;

public class KinectFittingRoom : MonoBehaviour {

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

    public Transform shoulderLCollider;
    public Transform shoulderRCollider;
    public Transform rightArmCollider;

    public Transform hipRightCollider;
    public Transform hipLeftCollider;
    public Transform handRightCollider;
    public Transform handLeftCollider;
    public Transform kneeRightCollider;
    public Transform kneeLeftCollider;
    public Transform bodyCollider; //用来撑住衣服使其不能乱飘向后面


    public float lerpSpeed = 3; //进行缓移关节的速度

    public ClothRenderer pantRenderer;
    public ClothRenderer shirtRenderer;
    public Texture2D[] pantTexs;
    public Texture2D[] shirtTexs;
    private int pantIndex=0;
    private int shirtIndex=0;
    private float lastChangeTime=0;

    public float zOffsetFromRGBScreen = 5; //全部关节都相对RGB屏幕沿Z轴偏移这个量
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


        bodyCollider.position = new Vector3(bodyCollider.position.x, bodyCollider.position.y, RGBRenderer.transform.position.z - zOffsetFromRGBScreen);
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
                Vector3 leftHandPos = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.HandLeft];
                Vector3 rightHandPos = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.HandRight];
                Vector3 leftShoulderPos = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.ShoulderLeft];
                Vector3 rightShoulderPos = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.ShoulderRight];
                if (leftHandPos.y > leftShoulderPos.y)
                    ChangePant();
                if (rightHandPos.y > rightShoulderPos.y)
                    ChangeShirt();

                SetLeftShoulderTransform(ref skeleton, ref shoulderLCollider);
                SetRightShoulderTransform(ref skeleton, ref shoulderRCollider);
                //SetRightArmTransform(ref skeleton, ref rightArmCollider);
                SetRightHipTransform(ref skeleton, ref hipRightCollider);
                SetLeftHipTransform(ref skeleton, ref hipLeftCollider);
                SetRightHandTransform(ref skeleton, ref handRightCollider);
                SetRightKneeTransform(ref skeleton, ref kneeRightCollider);
                SetLeftKneeTransform(ref skeleton, ref kneeLeftCollider);

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
    void SetLeftShoulderTransform(ref KinectWrapper.NuiSkeletonData skeleton, ref Transform jointCollider) //根据骨骼的位置来设置碰撞盒的位置
    {
        if (skeleton.eSkeletonPositionTrackingState[(int)KinectWrapper.NuiSkeletonPositionIndex.ShoulderLeft] != KinectWrapper.NuiSkeletonPositionTrackingState.NotTracked)
            
        {
            //这里我们根据shoulderCenter来设置面片的位置， 再根据spine和shoulderCenter构成的矢量偏离垂直方向的夹角来设置面片的欧拉角
            Vector3 shouderLeftPos = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.ShoulderLeft];
            
            Vector3 shoulderLeftScreenPos = GetJointInScreenPosition(shouderLeftPos);

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(shoulderLeftScreenPos.x, Screen.height - shoulderLeftScreenPos.y, RGBRenderer.transform.position.z)); //要用RGB图的Z值传进去才行
            float zOffSet = shouderLeftPos.z - skeleton.Position.z; 
            //这里直接用Kienct返回的Z值相减，显然不能和和上面几番转换得到的世界坐标坐标对应上，所以这里等增加一个比例系数试试
            zOffSet *= 15;
            Vector3 targetPos = new Vector3(worldPos.x, worldPos.y + 2.5f, RGBRenderer.transform.position.z + zOffSet - zOffsetFromRGBScreen); //只改变x y 坐标值
            jointCollider.position = Vector3.Lerp(jointCollider.position, targetPos, Time.deltaTime*lerpSpeed); 
           
        }
    }

    void SetRightShoulderTransform(ref KinectWrapper.NuiSkeletonData skeleton, ref Transform jointCollider) //根据骨骼的位置来设置碰撞盒的位置
    {
        if (skeleton.eSkeletonPositionTrackingState[(int)KinectWrapper.NuiSkeletonPositionIndex.ShoulderRight] != KinectWrapper.NuiSkeletonPositionTrackingState.NotTracked)
        {
           Vector3 shouderRightPos = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.ShoulderRight];

            Vector3 shoulderRightScreenPos = GetJointInScreenPosition(shouderRightPos);

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(shoulderRightScreenPos.x, Screen.height - shoulderRightScreenPos.y, RGBRenderer.transform.position.z)); //要用RGB图的Z值传进去才行
            float zOffset = shouderRightPos.z - skeleton.Position.z;
            //这里直接用Kienct返回的Z值相减，显然不能和和上面几番转换得到的世界坐标坐标对应上，所以这里等增加一个比例系数试试
            zOffset *= 15;
            
             Vector3 targetPos= new Vector3(worldPos.x, worldPos.y+2.5f, RGBRenderer.transform.position.z+zOffset-zOffsetFromRGBScreen); //只改变x y 坐标值
             jointCollider.position = Vector3.Lerp(jointCollider.position, targetPos, Time.deltaTime*lerpSpeed);                                             //2.5是为了不让collider遮挡到衣服做的偏移                   //加5是因为想让控制点不会陷入显示屏

        }
    }

    void SetRightHipTransform(ref KinectWrapper.NuiSkeletonData skeleton, ref Transform jointCollider) //根据骨骼的位置来设置碰撞盒的位置
    {
        if (skeleton.eSkeletonPositionTrackingState[(int)KinectWrapper.NuiSkeletonPositionIndex.HipRight] != KinectWrapper.NuiSkeletonPositionTrackingState.NotTracked)
        {
            Vector3 hipRightPos = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.HipRight];

            Vector3 hipRightScreenPos = GetJointInScreenPosition(hipRightPos);

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(hipRightScreenPos.x, Screen.height - hipRightScreenPos.y, RGBRenderer.transform.position.z)); //要用RGB图的Z值传进去才行
            float zOffset = hipRightPos.z - skeleton.Position.z;
            Vector3 targetPos = new Vector3(worldPos.x + 1.5f, worldPos.y, RGBRenderer.transform.position.z + zOffset-zOffsetFromRGBScreen); //只改变x y 坐标值
            jointCollider.position = Vector3.Lerp(jointCollider.position, targetPos, Time.deltaTime * lerpSpeed);

        }
    }

    void SetLeftHipTransform(ref KinectWrapper.NuiSkeletonData skeleton, ref Transform jointCollider) //根据骨骼的位置来设置碰撞盒的位置
    {
        if (skeleton.eSkeletonPositionTrackingState[(int)KinectWrapper.NuiSkeletonPositionIndex.HipLeft] != KinectWrapper.NuiSkeletonPositionTrackingState.NotTracked)
        {
            Vector3 hipLeftPos = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.HipLeft];

            Vector3 hipLeftScreenPos = GetJointInScreenPosition(hipLeftPos);

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(hipLeftScreenPos.x, Screen.height - hipLeftScreenPos.y, RGBRenderer.transform.position.z)); //要用RGB图的Z值传进去才行
            float zOffset = hipLeftPos.z - skeleton.Position.z;
            Vector3 targetPos = new Vector3(worldPos.x - 1.5f, worldPos.y, RGBRenderer.transform.position.z + zOffset-zOffsetFromRGBScreen); //只改变x y 坐标值
                                                        //1.5f 是为了实际臀部肉体边缘和臀关节的偏差
            jointCollider.position = Vector3.Lerp(jointCollider.position, targetPos, Time.deltaTime * lerpSpeed);

        }
    }

    void SetRightKneeTransform(ref KinectWrapper.NuiSkeletonData skeleton, ref Transform jointCollider) //根据骨骼的位置来设置碰撞盒的位置
    {
        if (skeleton.eSkeletonPositionTrackingState[(int)KinectWrapper.NuiSkeletonPositionIndex.KneeRight] != KinectWrapper.NuiSkeletonPositionTrackingState.NotTracked)
        {
            Vector3 kneeRightPos = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.KneeRight];

            Vector3 kneeRightScreenPos = GetJointInScreenPosition(kneeRightPos);

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(kneeRightScreenPos.x, Screen.height - kneeRightScreenPos.y, RGBRenderer.transform.position.z)); //要用RGB图的Z值传进去才行
            float zOffset = kneeRightPos.z - skeleton.Position.z;
            zOffset *= 20;
            jointCollider.position = new Vector3(worldPos.x, worldPos.y, RGBRenderer.transform.position.z + zOffset - zOffsetFromRGBScreen); //只改变x y 坐标值

        }
    }

    void SetLeftKneeTransform(ref KinectWrapper.NuiSkeletonData skeleton, ref Transform jointCollider) //根据骨骼的位置来设置碰撞盒的位置
    {
        if (skeleton.eSkeletonPositionTrackingState[(int)KinectWrapper.NuiSkeletonPositionIndex.KneeLeft] != KinectWrapper.NuiSkeletonPositionTrackingState.NotTracked)
        {
            Vector3 kneeLeftPos = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.KneeLeft];

            Vector3 kneeLeftScreenPos = GetJointInScreenPosition(kneeLeftPos);

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(kneeLeftScreenPos.x, Screen.height - kneeLeftScreenPos.y, RGBRenderer.transform.position.z)); //要用RGB图的Z值传进去才行
            float zOffset = kneeLeftPos.z - skeleton.Position.z;
            zOffset *= 20;
            jointCollider.position = new Vector3(worldPos.x, worldPos.y, RGBRenderer.transform.position.z + zOffset - zOffsetFromRGBScreen); //只改变x y 坐标值

        }
    }
    void SetRightHandTransform(ref KinectWrapper.NuiSkeletonData skeleton, ref Transform jointCollider) //根据骨骼的位置来设置碰撞盒的位置
    {
        if (skeleton.eSkeletonPositionTrackingState[(int)KinectWrapper.NuiSkeletonPositionIndex.HandRight] != KinectWrapper.NuiSkeletonPositionTrackingState.NotTracked)
        {
            Vector3 handRightPos = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.HandRight];

            Vector3 handRightScreenPos = GetJointInScreenPosition(handRightPos);

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(handRightScreenPos.x, Screen.height - handRightScreenPos.y, RGBRenderer.transform.position.z)); //要用RGB图的Z值传进去才行
            float zOffset = handRightPos.z - skeleton.Position.z;
            jointCollider.position = new Vector3(worldPos.x + 1.5f, worldPos.y, RGBRenderer.transform.position.z - zOffset-zOffsetFromRGBScreen-zOffsetFromRGBScreen); //只改变x y 坐标值

        }
    }
    void SetRightArmTransform(ref KinectWrapper.NuiSkeletonData skeleton, ref Transform armCollider)
    {
         //这里我们根据shoulderCenter来设置面片的位置， 再根据spine和shoulderCenter构成的矢量偏离垂直方向的夹角来设置面片的欧拉角
            Vector3 shouderRightPos = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.ShoulderRight];

            Vector3 shoulderRightScreenPos = GetJointInScreenPosition(shouderRightPos);

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(shoulderRightScreenPos.x, Screen.height - shoulderRightScreenPos.y, RGBRenderer.transform.position.z)); //要用RGB图的Z值传进去才行
            float zOffset = shouderRightPos.z - skeleton.Position.z;
            armCollider.position = new Vector3(worldPos.x, worldPos.y, RGBRenderer.transform.position.z+zOffset); //只改变x y 坐标值

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

    void ChangeShirt()
    {
        if (Time.realtimeSinceStartup - lastChangeTime > 2f)
        {
            shirtIndex++;
            if (shirtIndex >= shirtTexs.Length) shirtIndex = 0;
            shirtRenderer.material.mainTexture = shirtTexs[shirtIndex];
            lastChangeTime = Time.realtimeSinceStartup;
        }

    }

    void ChangePant()
    {
        if (Time.realtimeSinceStartup - lastChangeTime > 2f)
        {
            pantIndex++;
            if (pantIndex >= pantTexs.Length) pantIndex = 0;
            pantRenderer.material.mainTexture = pantTexs[pantIndex];
            lastChangeTime = Time.realtimeSinceStartup;
        }

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
