using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

public class GripGestureTest : MonoBehaviour {
    public GUIText tip;
    public bool NearMode = true;
    public int maxOffset = 100;   //要算为有效点可以离最近点的最大距离
    private IntPtr depthStreamHandle;
    private short[] depthMap;  //深度信息的数组
    private int imageWidth, imageHeight;
    private Texture2D handTexture; // 只显示前面部分的图

	// Use this for initialization
	void Start () 
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
        List<int> userPointDist = new List<int>(); //记录所有属于用户点的距离数据

        int closestDistance = 8000;
        for (int i = 0; i <  depthMap.Length; i++) //第一遍遍历数组求出最近点
        {   
            if((depthMap[i] & 7) !=0)              //只算属于人物的点
            {
                int dist=(depthMap[i] >> 3);
                userPointDist.Add(dist);           //记录用户点距离
                if (dist < closestDistance && dist!=0)
                {
                    closestDistance = dist;
                }
            }
        }

        tip.text = "最近点：" + closestDistance+"/n 在范围点数："+PointsInRange(ref userPointDist,closestDistance,maxOffset);
        //得出结果是0，怎么回事?   加上是0就舍弃就可以了
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
                else                        //不在范围的人物像素也为黑色
                {
                    colors[flipIndex] = Color.black;
                }
            }
        }

        handTexture.SetPixels(colors);
        handTexture.Apply();
    }
    int PointsInRange(ref List<int> points, int minDistance, int maxOffset) //算出所有的人物点在范围内的个数
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
