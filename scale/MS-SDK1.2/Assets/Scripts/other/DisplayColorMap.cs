using UnityEngine;
using System.Collections;

using System;
[RequireComponent(typeof(Renderer))]
public class DisplayColorMap : MonoBehaviour 
{
    private IntPtr colorStreamHandle;// Image stream handles for the kinect
    private int imageWidth,imageHeight;
    private Color32[] colorImage;    // Color image data, if used
    private Texture2D colorMap;      //我们用来显示的彩色图

    void Start()
    {
        #region 用KinectWrapper进行各种Nui的初始化
        int hr=0; //用来存储各种函数返回值
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
	void Update () {        
        if (colorStreamHandle != IntPtr.Zero && KinectWrapper.PollColor(colorStreamHandle, ref colorImage))//第二个是你想用来接收刚更新好的颜色图的Color32数组
        {
            UpdateColorMap();
        }

	}

    // Update the Color Map
    void UpdateColorMap()
    {
        #region Whatever colorMap tweaking you want to do here
         //colorImage = redOnly(colorImage, colorMap.width, colorMap.height);
        //colorImage = ApocalypticZombie(colorImage);
        //LeftMirror(ref colorImage);
        #endregion

        //要是想要直接获取原始图像，就把上面的加工函数删掉
        colorMap.SetPixels32(colorImage);
        colorMap.Apply();

        MirrorTexture(colorMap);
    }

    #region Colors-tweaking Methos 


    Color32[] redOnly(Color32[] src, int width, int height)
    {
        Color32[] newColors = new Color32[src.Length];

        for (int i = 0; i < src.Length;i++ )
        {
            newColors[i] = src[i];
            newColors[i].b = 0;
            newColors[i].g = 0;
        }

        return newColors;
    }
    //Apocalyptic Zombie – Invert the red pixel and swap the blue and green values.
    Color32[] ApocalypticZombie(Color32[] src)
    {
        Color32[] newColors = new Color32[src.Length];
        for (int i = 0; i < src.Length; i++)
        {
            newColors[i] = src[i];
            newColors[i].r = (byte)~newColors[i].r; //Color32颜色分量r g b a 都是byte类型，所以可以这样直接进行位运算
            //byte temp = newColors[i].b;
            newColors[i].b = newColors[i].g;
            //newColors[i].g = temp;
        }

        return newColors;
    }

    void MirrorTexture(Texture2D tex) //原理是这样，需要的再扩展为上下镜像 左右 随意选，动态结合使用
    {
        int width = tex.width;
        int height = tex.height;
        for (int x = width / 2; x < width; x++)
        {
            for (int y = 1; y < height; y++)
            {
                Color leftColor = tex.GetPixel(width - x, y);
                tex.SetPixel(x, y, leftColor);
            }
        }

        tex.Apply();
    }

    void LeftMirror(ref Color32[] src)//把颜色数组进行镜像复制
    {
        for (int x = 0; x < imageWidth; x++)
        {
            for (int y = 0; y < imageHeight; y++)
            {
                if (x > imageWidth * 0.5) //只是对图像右边的颜色进行处理
                {
                    //src[y * imageWidth + x] = src[y * imageWidth + imageWidth - x];
                }
            }
        }
    }
    // 为了提高性能时可用 指针操作
    //Unity报错的解决办法：Assets目录下面添加smcs.rsp文件，里面只加一行字不要有空格  -unsafe。 OK搞定。记得一定要重启Unity3d, 因为这个预编译是在启动U3D时候运行的
    /*unsafe void reflectImage(byte[] colorData, int width, int height)
    {
        fixed (byte* imageBase = colorData)
        {
            // Get the base position as an integer pointer
            int* imagePosition = (int*)imageBase;
            // repeat for each row
            for (int row = 0; row < height; row++)
            {
                // read from the left edge
                int* fromPos = imagePosition + (row * width);
                // write to the right edge
                int* toPos = fromPos + width - 1;
                while (fromPos < toPos)
                {
                    *toPos = *fromPos; // copy the pixel
                    fromPos++; // move towards the middle
                    toPos--; // move back from the right edge
                }
            }
        }
    }*/
     
    #endregion Colors-Tweaking Methos
    void OnApplicationQuit() //程序结束要做些收尾工作，不然Kinect不会自动关闭
    {
        KinectWrapper.NuiShutdown();
    }
}
