using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

public class GripGestureTest : MonoBehaviour {
    public GUIText tip;
    public bool NearMode = true;
    public int maxOffset = 100;   //Ҫ��Ϊ��Ч�������������������
    private IntPtr depthStreamHandle;
    private short[] depthMap;  //�����Ϣ������
    private int imageWidth, imageHeight;
    private Texture2D handTexture; // ֻ��ʾǰ�沿�ֵ�ͼ

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

        imageHeight = KinectWrapper.GetDepthHeight();
        imageWidth = KinectWrapper.GetDepthWidth();
        depthMap = new short[imageWidth * imageHeight];
        handTexture = new Texture2D(imageWidth, imageHeight);

        tip = GetComponent<GUIText>();
	}
	
	// Update is called once per frame
	void Update () 
    {
        if (depthStreamHandle != IntPtr.Zero && KinectWrapper.PollDepth(depthStreamHandle, NearMode, ref depthMap))
        {
            DetectGripGesture();
        }
	}


    void DetectGripGesture()
    {
        List<int> userPointDist = new List<int>(); //��¼���������û���ľ�������

        int closestDistance = 8000;
        for (int i = 0; i <  depthMap.Length; i++) //��һ�����������������
        {   
            if((depthMap[i] & 7) !=0)              //ֻ����������ĵ�
            {
                int dist=(depthMap[i] >> 3);
                userPointDist.Add(dist);           //��¼�û������
                if (dist < closestDistance && dist!=0)
                {
                    closestDistance = dist;
                }
            }
        }

        tip.text = "����㣺" + closestDistance+"/n �ڷ�Χ������"+PointsInRange(ref userPointDist,closestDistance,maxOffset);
        //�ó������0����ô����?   ������0�������Ϳ�����
        UpdateHandTexture(closestDistance,maxOffset);

    }

    void UpdateHandTexture(int closestDistance,int maxOffset)
    {
        Color[] colors = new Color[depthMap.Length];
        for (int i = 0; i < depthMap.Length; i++)
        {
            int flipIndex = depthMap.Length - i - 1;
            if ((depthMap[i] & 7) == 0)
            {
                colors[flipIndex] = Color.black;
            }
            else
            {
                int depth = depthMap[i] >> 3;
                if (depth < closestDistance + maxOffset && depth != 0)
                {
                    colors[flipIndex] = Color.blue;
                }
                else                        //���ڷ�Χ����������ҲΪ��ɫ
                {
                    colors[flipIndex] = Color.black;
                }
            }
        }

        handTexture.SetPixels(colors);
        handTexture.Apply();
    }
    int PointsInRange(ref List<int> points, int minDistance, int maxOffset) //������е�������ڷ�Χ�ڵĸ���
    {
        int count = 0;
        for (int i = 0; i < points.Count; i++)
        {
            if (points[i] < minDistance + maxOffset) count++;     
        }

        return count;
    }

    void OnGUI()
    {
        GUILayout.Button(handTexture);
    }
    void OnApplicationQuit()
    {
        KinectWrapper.NuiShutdown();
    }
}
