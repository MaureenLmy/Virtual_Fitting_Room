using UnityEngine;
using System.Collections;
using System;
public class RGBImage : MonoBehaviour {

    private IntPtr colorStreamHandle;// Image stream handles for the kinect
    private int imageWidth, imageHeight;
    private Color32[] colorImage;    // Color image data, if used
    private Texture2D colorMap;      //我们用来显示的彩色图

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
        renderer.material.mainTexture = colorMap;

    }

    // Update is called once per frame
    void Update()
    {
        if (colorStreamHandle != IntPtr.Zero && KinectWrapper.PollColor(colorStreamHandle, ref colorImage))//第二个是你想用来接收刚更新好的颜色图的Color32数组
        {
            UpdateColorMap();
        }

        if (Input.GetKey(KeyCode.Escape)) Application.Quit();
    }

    // Update the Color Map
    void UpdateColorMap()
    {

        //要是想要直接获取原始图像，就把上面的加工函数删掉
        colorMap.SetPixels32(colorImage);
        colorMap.Apply();
    }
    
    void OnApplicationQuit() //程序结束要做些收尾工作，不然Kinect不会自动关闭
    {
        KinectWrapper.NuiShutdown();
    }
}
