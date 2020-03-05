using UnityEngine;
using System.Collections;
using System;
public class RGBImage : MonoBehaviour {

    private IntPtr colorStreamHandle;// Image stream handles for the kinect
    private int imageWidth, imageHeight;
    private Color32[] colorImage;    // Color image data, if used
    private Texture2D colorMap;      //����������ʾ�Ĳ�ɫͼ

    void Start()
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
    void Update()
    {
        if (colorStreamHandle != IntPtr.Zero && KinectWrapper.PollColor(colorStreamHandle, ref colorImage))//�ڶ����������������ոո��ºõ���ɫͼ��Color32����
        {
            UpdateColorMap();
        }

        if (Input.GetKey(KeyCode.Escape)) Application.Quit();
    }

    // Update the Color Map
    void UpdateColorMap()
    {

        //Ҫ����Ҫֱ�ӻ�ȡԭʼͼ�񣬾Ͱ�����ļӹ�����ɾ��
        colorMap.SetPixels32(colorImage);
        colorMap.Apply();
    }
    
    void OnApplicationQuit() //�������Ҫ��Щ��β��������ȻKinect�����Զ��ر�
    {
        KinectWrapper.NuiShutdown();
    }
}
