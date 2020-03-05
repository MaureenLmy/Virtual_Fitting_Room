using UnityEngine;
using System.Collections;
using System;

public class DisplayDepthMap : MonoBehaviour {
    public bool NearMode=true;
    public bool useRawDepth = true; //要完整的深度图还是只有人物的深度图


    private IntPtr depthStreamHandle;
    private short[] usersDepthMap;
    private float[] usersHistogramMap;
    // User Map vars.   
    public Texture2D usersLblTex;
    private Color[] usersMapColors;
    private int usersMapSize;
	// Use this for initialization
	void Start ()
    {
        #region 初始化Nui和深度流
        int hr = 0;
        try
        {
            hr = KinectWrapper.NuiInitialize(KinectWrapper.NuiInitializeFlags.UsesDepthAndPlayerIndex |
                KinectWrapper.NuiInitializeFlags.UsesSkeleton | KinectWrapper.NuiInitializeFlags.UsesColor);
            if (hr != 0)
            {
                throw new Exception("NuiInitialize Failed");
            }

            depthStreamHandle = IntPtr.Zero;
            hr = KinectWrapper.NuiImageStreamOpen(KinectWrapper.NuiImageType.DepthAndPlayerIndex,
                KinectWrapper.Constants.ImageResolution, 0, 2, IntPtr.Zero, ref depthStreamHandle);
            if (hr != 0)
            {
                throw new Exception("Cannot open depth stream");
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message + " - " + KinectWrapper.GetNuiErrorString(hr));
            return;
        }
        #endregion

        // Initialize depth & label map related stuff
        usersMapSize = KinectWrapper.GetDepthWidth() * KinectWrapper.GetDepthHeight();
        usersLblTex = new Texture2D(KinectWrapper.GetDepthWidth(), KinectWrapper.GetDepthHeight());
        usersMapColors = new Color[usersMapSize];

        usersDepthMap = new short[usersMapSize];
        usersHistogramMap = new float[5000];

        //用来显示
        renderer.material.mainTexture = usersLblTex;
        
    }
	
	// Update is called once per frame
	void Update () 
    {
        if (depthStreamHandle != IntPtr.Zero &&  KinectWrapper.PollDepth(depthStreamHandle, NearMode, ref usersDepthMap))
        {
            if (useRawDepth)
            {
                UpdateRawDepthMap();
            }
            else
            {
                
                UpdateUserMap();
            }
            
            
        }
	}
    // Update the User Map
    void UpdateUserMap()
    {
        // Flip the texture as we convert label map to color array
        int flipIndex, i;
        int numOfPoints = 0;
        Array.Clear(usersHistogramMap, 0, usersHistogramMap.Length);

        // Calculate cumulative histogram for depth
        for (i = 0; i < usersMapSize; i++)
        {
            // Only calculate for depth that contains users
            if ((usersDepthMap[i] & 7) != 0) //用这个来判断改点是否属于能追踪的人物， 还没看懂
            {                                //7 用二进制就是111，前三位为人物索引：The playerNo value is obtained by using the & (arithmetic AND) operation to split off the bottom 3
                                             //bits of the value in the depth array. This works because the binary representation of seven is 111.
                usersHistogramMap[usersDepthMap[i] >> 3]++;//Histogram:柱状图，直方图，这句可留可不留，留着的话，可以是人物身上的颜色根据深度有深浅变化
                numOfPoints++;
            }
        }

        if (numOfPoints > 0)
        {
            for (i = 1; i < usersHistogramMap.Length; i++)
            {
                usersHistogramMap[i] += usersHistogramMap[i - 1];//把属于人物的点的的值都设为与前一个点的和（当然不算第一点）
            }

            for (i = 0; i < usersHistogramMap.Length; i++)
            {
                usersHistogramMap[i] = 1.0f - (usersHistogramMap[i] / numOfPoints);
                //怎么理解这行的作用？
            }
        }

        // Create the actual users texture based on label map and depth histogram
        for (i = 0; i < usersMapSize; i++)
        {
            flipIndex = usersMapSize - i - 1; //这句和下一句可以把图像的上下颠倒，根据需要自己可二选一
            //flipIndex = i;
            short userMap = (short)(usersDepthMap[i] & 7);//7 在这里代替微软提供的SDK的 DepthImageFrame.PlayerIndexBitmaskWidth;
                                                          //1.7版本的SDK最多只能追踪6个人，所以这个值不是0 的话就是1-6 代表人物编号
            short userDepth = (short)(usersDepthMap[i] >> 3);//DepthImageFrame.PlayerIndexBitmask

            if (userMap == 0) //不是属于人物的点
            {
                usersMapColors[flipIndex] = Color.clear;//Completely transparent. RGBA is (0, 0, 0, 0)

            }
            else            //属于人物的点
            {
                // Create a blending color based on the depth histogram
                float histDepth = usersHistogramMap[userDepth];
                Color c = new Color(histDepth, histDepth, histDepth, 0.9f);

                switch (userMap % 4)
                {
                    case 0:
                        usersMapColors[flipIndex] = Color.red * c;
                        break;
                    case 1:
                        usersMapColors[flipIndex] = Color.green * c;
                        break;
                    case 2:
                        usersMapColors[flipIndex] = Color.blue * c;
                        break;
                    case 3:
                        usersMapColors[flipIndex] = Color.magenta * c;
                        break;
                }
            }
        }

        // Draw it!
        usersLblTex.SetPixels(usersMapColors);
        usersLblTex.Apply();
    }

    void UpdateRawDepthMap() //上面给的是挑选出了人物的高度图， 这里尝试获取原始的深度图
    {
        for (int i = 0; i < usersMapSize; i++)
        {    
            short Depth = (short)(usersDepthMap[i] >> 3);//DepthImageFrame.PlayerIndexBitmask

            float colorValue =(float) Depth/ 8000f;//返回的深度值单位好像是毫米，所以
            
            #region 想要分段显示颜色的话可以这样
            /*
            if (Depth > 3000)
            {
                usersMapColors[i] = Color.blue;
            }
            else if (Depth > 2000)
            {
                usersMapColors[i] = Color.green;
            }
            else
            {
                usersMapColors[i] = new Color(colorValue, colorValue, colorValue);
            }
             */
            #endregion
            usersMapColors[i] = new Color(colorValue, colorValue, colorValue);

        }

        // Draw it!
        usersLblTex.SetPixels(usersMapColors);
        usersLblTex.Apply();

    }

    void CheckForGripAndRelease() //这里来实现一个握拳和松开拳的检测
    {
        ////基本原理，用一个数组存储力摄像机最近的像素点的集合，也就只有手往前伸的时候会有作用
        // 接着握拳的时候就会有多个像素不再符合深度值，超过了阈值就视为握拳

    }
    void OnApplicationQuit() //程序结束要做些收尾工作，不然Kinect不会自动关闭
    {
        KinectWrapper.NuiShutdown();
    }
}
