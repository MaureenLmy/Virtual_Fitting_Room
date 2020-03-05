using UnityEngine;
using System.Collections;

using System;
[RequireComponent(typeof(Renderer))]
public class DisplayColorMap : MonoBehaviour 
{
    private IntPtr colorStreamHandle;// Image stream handles for the kinect
    private int imageWidth,imageHeight;
    private Color32[] colorImage;    // Color image data, if used
    private Texture2D colorMap;      //����������ʾ�Ĳ�ɫͼ

    void Start()
    {
        #region ��KinectWrapper���и���Nui�ĳ�ʼ��
        int hr=0; //�����洢���ֺ�������ֵ
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
            if (hr != 0)              //��ֱ������KinectWrapper���õ���ɫͼ������һֱʧ�ܣ� ����������û�г�ʼ���ã�
            {                         //��ǣ�ԭ����û���ȵ��� KinectWrapper.NuiInitialize�������г�ʼ��
                throw new Exception("Cannot open color stream");
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message + " - " + KinectWrapper.GetNuiErrorString(hr));
            return;
        }
        #endregion ��KinectWrapper���и���Nui�ĳ�ʼ��

        imageWidth = KinectWrapper.GetDepthWidth();
        imageHeight = KinectWrapper.GetDepthHeight();
        int usersMapSize = imageWidth * imageHeight;
        colorImage = new Color32[usersMapSize];
        colorMap = new Texture2D(imageWidth, imageHeight);
        renderer.material.mainTexture = colorMap;



    }

	// Update is called once per frame
	void Update () {        
        if (colorStreamHandle != IntPtr.Zero && KinectWrapper.PollColor(colorStreamHandle, ref colorImage))//�ڶ����������������ոո��ºõ���ɫͼ��Color32����
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

        //Ҫ����Ҫֱ�ӻ�ȡԭʼͼ�񣬾Ͱ�����ļӹ�����ɾ��
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
    //Apocalyptic Zombie �C Invert the red pixel and swap the blue and green values.
    Color32[] ApocalypticZombie(Color32[] src)
    {
        Color32[] newColors = new Color32[src.Length];
        for (int i = 0; i < src.Length; i++)
        {
            newColors[i] = src[i];
            newColors[i].r = (byte)~newColors[i].r; //Color32��ɫ����r g b a ����byte���ͣ����Կ�������ֱ�ӽ���λ����
            //byte temp = newColors[i].b;
            newColors[i].b = newColors[i].g;
            //newColors[i].g = temp;
        }

        return newColors;
    }

    void MirrorTexture(Texture2D tex) //ԭ������������Ҫ������չΪ���¾��� ���� ����ѡ����̬���ʹ��
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

    void LeftMirror(ref Color32[] src)//����ɫ������о�����
    {
        for (int x = 0; x < imageWidth; x++)
        {
            for (int y = 0; y < imageHeight; y++)
            {
                if (x > imageWidth * 0.5) //ֻ�Ƕ�ͼ���ұߵ���ɫ���д���
                {
                    //src[y * imageWidth + x] = src[y * imageWidth + imageWidth - x];
                }
            }
        }
    }
    // Ϊ���������ʱ���� ָ�����
    //Unity����Ľ���취��AssetsĿ¼�������smcs.rsp�ļ�������ֻ��һ���ֲ�Ҫ�пո�  -unsafe�� OK�㶨���ǵ�һ��Ҫ����Unity3d, ��Ϊ���Ԥ������������U3Dʱ�����е�
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
    void OnApplicationQuit() //�������Ҫ��Щ��β��������ȻKinect�����Զ��ر�
    {
        KinectWrapper.NuiShutdown();
    }
}
