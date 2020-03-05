using UnityEngine;
using System.Collections;
using System;


//��Ų����û����
public class UserHeightMeasure : MonoBehaviour 
{

    public bool NearMode = true;
    public bool useRawDepth = true; //Ҫ���������ͼ����ֻ����������ͼ


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
        #region ��ʼ��Nui�������
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

        //������ʾ
        renderer.material.mainTexture = usersLblTex;



        // System.Threading.Thread.Sleep(15000);  //ֻ����������һ���߳�
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

            float colorValue = (float)Depth / 8000f;//���ص����ֵ��λ�����Ǻ��ף�����

            usersMapColors[i] = new Color(colorValue, colorValue, colorValue);

        }

        // Draw it!
        usersLblTex.SetPixels(usersMapColors);
        usersLblTex.Apply();

    }

    private static readonly double HorizontalPoVTanA = Math.Tan(57.0 / 2.0 * Math.PI / 180); //��Ȼ�����������ʽ����ô������ˣ���������ĺ���Ҫ�õ�
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

        //�������ͼ��֡��ÿһ������
        for (int row = 0; row < imageHeight; row++)
        {
            for (int col = 0; col < imageWidth; col++)
            {
                pixelIndex = col + (row * imageWidth);
                depth = pixelData[pixelIndex] >> 3;   // DepthImageFrame.PlayerIndexBitmaskWidth;
                playerIndex = (pixelData[pixelIndex] & 7 );// DepthImageFrame.PlayerIndexBitmask);

                //ע���ô����֧��һ���û�����
                if (depth != 0 && playerIndex != 0)
                {
                    depthCount++;
                    depthSum += depth;
                    depthPixelBodyLeft = Math.Min(depthPixelBodyLeft, col); //�������ص�
                    depthPixelBodyRight = Math.Max(depthPixelBodyRight, col); //�������ص�
                    depthPixelBodyBottom = Math.Min(depthPixelBodyBottom, row); //������ص�
                    depthPixelBodyTop = Math.Max(depthPixelBodyTop, row); //������ص�
                }
            }
        }

        if (depthCount != 0)
        {
            double avgDepth = depthSum / depthCount;
            int pixelWidth = Math.Abs(depthPixelBodyRight - depthPixelBodyLeft);
            int pixelHeight = Math.Abs(depthPixelBodyTop - depthPixelBodyBottom);

            //�������Ǽ����Ƶ�
            double realHeightViaDepth = (2 * avgDepth * HorizontalPoVTanA * pixelHeight) / imageWidth;
            heightDisplayText.text = "��ߣ�" +realHeightViaDepth.ToString();
        }
    }
    void OnApplicationQuit() //�������Ҫ��Щ��β��������ȻKinect�����Զ��ر�
    {
        KinectWrapper.NuiShutdown();
    }
}
