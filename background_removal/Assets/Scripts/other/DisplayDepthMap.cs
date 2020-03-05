using UnityEngine;
using System.Collections;
using System;

public class DisplayDepthMap : MonoBehaviour {
    public bool NearMode=true;
    public bool useRawDepth = true; //Ҫ���������ͼ����ֻ����������ͼ


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
        usersMapSize = KinectWrapper.GetDepthWidth() * KinectWrapper.GetDepthHeight();
        usersLblTex = new Texture2D(KinectWrapper.GetDepthWidth(), KinectWrapper.GetDepthHeight());
        usersMapColors = new Color[usersMapSize];

        usersDepthMap = new short[usersMapSize];
        usersHistogramMap = new float[5000];

        //������ʾ
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
            if ((usersDepthMap[i] & 7) != 0) //��������жϸĵ��Ƿ�������׷�ٵ���� ��û����
            {                                //7 �ö����ƾ���111��ǰ��λΪ����������The playerNo value is obtained by using the & (arithmetic AND) operation to split off the bottom 3
                                             //bits of the value in the depth array. This works because the binary representation of seven is 111.
                usersHistogramMap[usersDepthMap[i] >> 3]++;//Histogram:��״ͼ��ֱ��ͼ���������ɲ��������ŵĻ����������������ϵ���ɫ�����������ǳ�仯
                numOfPoints++;
            }
        }

        if (numOfPoints > 0)
        {
            for (i = 1; i < usersHistogramMap.Length; i++)
            {
                usersHistogramMap[i] += usersHistogramMap[i - 1];//����������ĵ�ĵ�ֵ����Ϊ��ǰһ����ĺͣ���Ȼ�����һ�㣩
            }

            for (i = 0; i < usersHistogramMap.Length; i++)
            {
                usersHistogramMap[i] = 1.0f - (usersHistogramMap[i] / numOfPoints);
                //��ô������е����ã�
            }
        }

        // Create the actual users texture based on label map and depth histogram
        for (i = 0; i < usersMapSize; i++)
        {
            flipIndex = usersMapSize - i - 1; //������һ����԰�ͼ������µߵ���������Ҫ�Լ��ɶ�ѡһ
            //flipIndex = i;
            short userMap = (short)(usersDepthMap[i] & 7);//7 ���������΢���ṩ��SDK�� DepthImageFrame.PlayerIndexBitmaskWidth;
                                                          //1.7�汾��SDK���ֻ��׷��6���ˣ��������ֵ����0 �Ļ�����1-6 ����������
            short userDepth = (short)(usersDepthMap[i] >> 3);//DepthImageFrame.PlayerIndexBitmask

            if (userMap == 0) //������������ĵ�
            {
                usersMapColors[flipIndex] = Color.clear;//Completely transparent. RGBA is (0, 0, 0, 0)

            }
            else            //��������ĵ�
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

    void UpdateRawDepthMap() //�����������ѡ��������ĸ߶�ͼ�� ���ﳢ�Ի�ȡԭʼ�����ͼ
    {
        for (int i = 0; i < usersMapSize; i++)
        {    
            short Depth = (short)(usersDepthMap[i] >> 3);//DepthImageFrame.PlayerIndexBitmask

            float colorValue =(float) Depth/ 8000f;//���ص����ֵ��λ�����Ǻ��ף�����
            
            #region ��Ҫ�ֶ���ʾ��ɫ�Ļ���������
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

    void CheckForGripAndRelease() //������ʵ��һ����ȭ���ɿ�ȭ�ļ��
    {
        ////����ԭ����һ������洢���������������ص�ļ��ϣ�Ҳ��ֻ������ǰ���ʱ���������
        // ������ȭ��ʱ��ͻ��ж�����ز��ٷ������ֵ����������ֵ����Ϊ��ȭ

    }
    void OnApplicationQuit() //�������Ҫ��Щ��β��������ȻKinect�����Զ��ر�
    {
        KinectWrapper.NuiShutdown();
    }
}
