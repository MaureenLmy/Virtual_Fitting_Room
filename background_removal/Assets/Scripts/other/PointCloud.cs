using UnityEngine;
using System.Collections;
using System;
using System.IO;

public class PointCloud : MonoBehaviour {
    public bool NearMode = true;
    private IntPtr depthStreamHandle;
    private short[] depthMap;  //深度信息的数组
    private int imageWidth, imageHeight;
   
    private bool firstTime = true;

    private ParticleEmitter emitter;
    private Particle[] points;


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

        imageHeight = KinectWrapper.GetDepthHeight();
        imageWidth = KinectWrapper.GetDepthWidth();
        depthMap = new short[imageWidth*imageHeight];
        

    }

    // Update is called once per frame
    void Update()
    {
        if (!firstTime) return;
        if (depthStreamHandle != IntPtr.Zero && KinectWrapper.PollDepth(depthStreamHandle, NearMode, ref depthMap))
        {
            // UpdatePS();//更新点云，这里用粒子系统来表现
             firstTime = false;
        }
    }

    void UpdatePS()   //更新由点云构造的粒子系统
    {
        emitter = GetComponent<ParticleEmitter>();
        int step = 10;
        points = new Particle[depthMap.Length / (step*step)];
        emitter.Emit(points.Length);
        int index = 0;
        for (int x = 0; x < imageWidth; x+=step)
        {
            for (int y = 0; y < imageHeight; y+=step)
            {
                int pos=y*imageWidth+x;
                points[index].position = DepthPoint2Position(depthMap[pos], pos)*0.01f;
                print(points[index].position);
                index++;
            }
        }
        emitter.particles = points;
        
    }

    
    Vector3 DepthPoint2Position(short depthPoint, int index)
    {
        int  z=(short)(depthPoint >> 3);//DepthImageFrame.PlayerIndexBitmask
        
        int  x = (index % imageWidth);
        int  y = (index / imageWidth);
        return new Vector3(x, y, z);

    }

    void SaveTextureToFile(Texture2D tex,string fileName)
    {
        var bytes=tex.EncodeToPNG();
        var file=File.Open(Application.dataPath+"/"+fileName,FileMode.Create);
        var binary=new BinaryWriter(file);
        binary.Write(bytes);
        file.Close();
    }

    
 
 
    void OnApplicationQuit() 
    {
        KinectWrapper.NuiShutdown();
    }
}
