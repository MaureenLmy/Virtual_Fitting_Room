using UnityEngine;
using System.Collections;
using System;

public class ARUserMap : MonoBehaviour {
    private IntPtr colorStreamHandle;// Image stream handles for the kinect
    private int imageWidth, imageHeight, mapSize;
    private Color32[] colorImage;    // Color image data, if used
    public  Texture2D colorMap;      //我们用来显示的彩色图
    public bool useMirrorImage = true; //是否把图像进行左右镜像

    public bool NearMode = true;
    private IntPtr depthStreamHandle;
    private short[] usersDepthMap;

    //y用来显示的地方
    public Renderer colorImageRenderer;
    public Renderer depthImageRenderer;

    public int pixelOffset; // 用来把扣图的位置向Y轴上以，以解决错位的问题
	// Use this for initialization
	void Start () 
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
           
            colorStreamHandle = IntPtr.Zero;
            hr = KinectWrapper.NuiImageStreamOpen(KinectWrapper.NuiImageType.Color, KinectWrapper.Constants.ImageResolution, 0, 2, IntPtr.Zero, ref colorStreamHandle);
            if (hr != 0)            
            {                         
                throw new Exception("Cannot open color stream");
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
        #endregion 用KinectWrapper进行各种Nui的初始化


        imageWidth = KinectWrapper.GetDepthWidth();
        imageHeight = KinectWrapper.GetDepthHeight();
        mapSize = imageWidth * imageHeight;
        colorImage = new Color32[mapSize];
        colorMap = new Texture2D(imageWidth, imageHeight);


        usersDepthMap = new short[mapSize];

        pixelOffset = imageWidth * 20; //这代表偏移的行数
        
	}
	
	// Update is called once per frame
	void Update () 
    {
        if (depthStreamHandle != IntPtr.Zero && KinectWrapper.PollDepth(depthStreamHandle, NearMode, ref usersDepthMap))
        {    
        }

        if (colorStreamHandle != IntPtr.Zero && KinectWrapper.PollColor(colorStreamHandle, ref colorImage))//第二个是你想用来接收刚更新好的颜色图的Color32数组
        {
            UpdateColorMap();
        }

        
	}



    // Update the Color Map
    void UpdateColorMap()
    {
        for (int i = 0; i < colorImage.Length; i++)
        {
            short userNumber = (short)(usersDepthMap[i] & 7); //由深度图返回的信息判断当前点是否属于人体
            if (userNumber == 0)
            {
                int flipIndex = colorImage.Length - i - 1 - pixelOffset; //这样做是因为深度图返回的点顺序和彩色图的顺序是相反的
                ///int flipIndex = ReverseMapIndex(i,imageWidth,imageHeight);
                if (flipIndex < 0 || flipIndex > colorImage.Length - 1)
                {
                    colorImage[i] = Color.clear;
                    continue;//
                }

                colorImage[flipIndex] = Color.clear;
                //colorImage[i] = Color.clear;
            }
            
        }

        //要是想要直接获取原始图像，就把上面的加工函数删掉
        colorMap.SetPixels32(colorImage);
        colorMap.Apply();
    }

    //把180度颠倒的width X height图的点索引转换的函数
    int ReverseMapIndex(int index, int width, int height)
    {
        int reverseIndex=(width*width-(index/width)*width+height-index%width);
        //C# 里好像没有整除运算符，所以这里的除号就当作是整除来用了
        return reverseIndex-1;
        
    }
    void OnGUI()
    {
        if (useMirrorImage)
            GUI.DrawTexture(new Rect(imageWidth, 0, -imageWidth, imageHeight), colorMap);
        else
            GUI.DrawTexture(new Rect(0, 0, imageWidth, imageHeight), colorMap);
    }
   
    void OnApplicationQuit() //程序结束要做些收尾工作，不然Kinect不会自动关闭
    {
        KinectWrapper.NuiShutdown();
    }
}
