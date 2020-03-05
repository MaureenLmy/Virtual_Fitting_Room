using UnityEngine;
using System.Collections;
using System;

public class ARUserMap : MonoBehaviour {
    private IntPtr colorStreamHandle;// Image stream handles for the kinect
    private int imageWidth, imageHeight, mapSize;
    private Color32[] colorImage;    // Color image data, if used
    public  Texture2D colorMap;      //����������ʾ�Ĳ�ɫͼ
    public bool useMirrorImage = true; //�Ƿ��ͼ��������Ҿ���

    public bool NearMode = true;
    private IntPtr depthStreamHandle;
    private short[] usersDepthMap;

    //y������ʾ�ĵط�
    public Renderer colorImageRenderer;
    public Renderer depthImageRenderer;

    public int pixelOffset; // �����ѿ�ͼ��λ����Y�����ԣ��Խ����λ������
	// Use this for initialization
	void Start () 
    {
        #region ��KinectWrapper���и���Nui�ĳ�ʼ��
        int hr = 0; //�����洢���ֺ�������ֵ
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
        #endregion ��KinectWrapper���и���Nui�ĳ�ʼ��


        imageWidth = KinectWrapper.GetDepthWidth();
        imageHeight = KinectWrapper.GetDepthHeight();
        mapSize = imageWidth * imageHeight;
        colorImage = new Color32[mapSize];
        colorMap = new Texture2D(imageWidth, imageHeight);


        usersDepthMap = new short[mapSize];

        pixelOffset = imageWidth * 20; //�����ƫ�Ƶ�����
        
	}
	
	// Update is called once per frame
	void Update () 
    {
        if (depthStreamHandle != IntPtr.Zero && KinectWrapper.PollDepth(depthStreamHandle, NearMode, ref usersDepthMap))
        {    
        }

        if (colorStreamHandle != IntPtr.Zero && KinectWrapper.PollColor(colorStreamHandle, ref colorImage))//�ڶ����������������ոո��ºõ���ɫͼ��Color32����
        {
            UpdateColorMap();
        }

        
	}



    // Update the Color Map
    void UpdateColorMap()
    {
        for (int i = 0; i < colorImage.Length; i++)
        {
            short userNumber = (short)(usersDepthMap[i] & 7); //�����ͼ���ص���Ϣ�жϵ�ǰ���Ƿ���������
            if (userNumber == 0)
            {
                int flipIndex = colorImage.Length - i - 1 - pixelOffset; //����������Ϊ���ͼ���صĵ�˳��Ͳ�ɫͼ��˳�����෴��
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

        //Ҫ����Ҫֱ�ӻ�ȡԭʼͼ�񣬾Ͱ�����ļӹ�����ɾ��
        colorMap.SetPixels32(colorImage);
        colorMap.Apply();
    }

    //��180�ȵߵ���width X heightͼ�ĵ�����ת���ĺ���
    int ReverseMapIndex(int index, int width, int height)
    {
        int reverseIndex=(width*width-(index/width)*width+height-index%width);
        //C# �����û���������������������ĳ��ž͵���������������
        return reverseIndex-1;
        
    }
    void OnGUI()
    {
        if (useMirrorImage)
            GUI.DrawTexture(new Rect(imageWidth, 0, -imageWidth, imageHeight), colorMap);
        else
            GUI.DrawTexture(new Rect(0, 0, imageWidth, imageHeight), colorMap);
    }
   
    void OnApplicationQuit() //�������Ҫ��Щ��β��������ȻKinect�����Զ��ر�
    {
        KinectWrapper.NuiShutdown();
    }
}
