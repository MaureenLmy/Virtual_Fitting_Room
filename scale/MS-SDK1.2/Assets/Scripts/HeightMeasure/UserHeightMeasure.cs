using UnityEngine;
using System.Collections;
using System;


//大概测试用户身高
public class UserHeightMeasure : MonoBehaviour 
{

    public bool NearMode = true;
    public bool useRawDepth = true; //要完整的深度图还是只有人物的深度图


    private IntPtr depthStreamHandle;
    private short[] usersDepthMap;
    private float[] usersHistogramMap;
    private int imageWidth;
    private int imageHeight;

    // User Map vars.   
    public Texture2D usersLblTex;
    private Color[] usersMapColors;
    private int usersMapSize;

    public GUIText heightDisplayText;
    // Use this for initialization
    void Start()
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
        imageWidth = KinectWrapper.GetDepthWidth();
        imageHeight = KinectWrapper.GetDepthHeight();
        usersMapSize = imageHeight * imageWidth;
        usersLblTex = new Texture2D(imageWidth, imageHeight);
        usersMapColors = new Color[usersMapSize];

        usersDepthMap = new short[usersMapSize];
        usersHistogramMap = new float[5000];

        //用来显示
        renderer.material.mainTexture = usersLblTex;



        // System.Threading.Thread.Sleep(15000);  //只是在这里试一试线程
    }

    // Update is called once per frame
    void Update()
    {
        if (depthStreamHandle != IntPtr.Zero && KinectWrapper.PollDepth(depthStreamHandle, NearMode, ref usersDepthMap))
        {
            if (useRawDepth)
            {
                UpdateRawDepthMap();
            }

            MessureSpecifiedPlayerSize(usersDepthMap);

        }
    }
 

    void UpdateRawDepthMap() 
    {
        for (int i = 0; i < usersMapSize; i++)
        {
            short Depth = (short)(usersDepthMap[i] >> 3);//DepthImageFrame.PlayerIndexBitmask

            float colorValue = (float)Depth / 8000f;//返回的深度值单位好像是毫米，所以

            usersMapColors[i] = new Color(colorValue, colorValue, colorValue);

        }

        // Draw it!
        usersLblTex.SetPixels(usersMapColors);
        usersLblTex.Apply();

    }

    private static readonly double HorizontalPoVTanA = Math.Tan(57.0 / 2.0 * Math.PI / 180); //虽然不明白这个公式是怎么算出来了，但是下面的函数要用到
    private void MessureSpecifiedPlayerSize( short[] pixelData)
    {
        int depth;
        int playerIndex;
        int pixelIndex;
        //int bytesPerPixel = depthFrame.BytesPerPixel;

        int depthSum = 0;
        int depthCount = 0;
        int depthPixelBodyLeft = int.MaxValue;
        int depthPixelBodyRight = int.MinValue;
        int depthPixelBodyBottom = int.MaxValue;
        int depthPixelBodyTop = int.MinValue;

        //遍历深度图像帧的每一个像素
        for (int row = 0; row < imageHeight; row++)
        {
            for (int col = 0; col < imageWidth; col++)
            {
                pixelIndex = col + (row * imageWidth);
                depth = pixelData[pixelIndex] >> 3;   // DepthImageFrame.PlayerIndexBitmaskWidth;
                playerIndex = (pixelData[pixelIndex] & 7 );// DepthImageFrame.PlayerIndexBitmask);

                //注：该代码仅支持一个用户测量
                if (depth != 0 && playerIndex != 0)
                {
                    depthCount++;
                    depthSum += depth;
                    depthPixelBodyLeft = Math.Min(depthPixelBodyLeft, col); //最左像素点
                    depthPixelBodyRight = Math.Max(depthPixelBodyRight, col); //最右像素点
                    depthPixelBodyBottom = Math.Min(depthPixelBodyBottom, row); //最低像素点
                    depthPixelBodyTop = Math.Max(depthPixelBodyTop, row); //最高像素点
                }
            }
        }

        if (depthCount != 0)
        {
            double avgDepth = depthSum / depthCount;
            int pixelWidth = Math.Abs(depthPixelBodyRight - depthPixelBodyLeft);
            int pixelHeight = Math.Abs(depthPixelBodyTop - depthPixelBodyBottom);

            //根据三角几何推导
            double realHeightViaDepth = (2 * avgDepth * HorizontalPoVTanA * pixelHeight) / imageWidth;
            heightDisplayText.text = "身高：" +realHeightViaDepth.ToString();
        }
    }
    void OnApplicationQuit() //程序结束要做些收尾工作，不然Kinect不会自动关闭
    {
        KinectWrapper.NuiShutdown();
    }
}
