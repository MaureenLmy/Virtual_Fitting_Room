using UnityEngine;
using System.Collections;

using System;
using System.IO;
public class GeneraHighMapFromDepth : MonoBehaviour {

    public bool NearMode = true;
    public Material mapMaterial;
    private IntPtr depthStreamHandle;
    private short[] depthMap;  //深度信息的数组
    private int imageWidth, imageHeight;

    private bool firstTime = true;


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
        depthMap = new short[imageWidth * imageHeight];


    }

    // Update is called once per frame
    void Update()
    {
        if (!firstTime) return;
        if (depthStreamHandle != IntPtr.Zero && KinectWrapper.PollDepth(depthStreamHandle, NearMode, ref depthMap))
        {
            GenerateHighMap();
            firstTime = false;
        }
    }

    void GenerateHighMap()
    {
        Color[] mapColor = new Color[depthMap.Length];
        for (int i = 0; i < mapColor.Length; i++)
        {
            float Depth = (float)(depthMap[i] >> 3) / 8000;//DepthImageFrame.PlayerIndexBitmask
            mapColor[i] = new Color(Depth, Depth, Depth);
        }
        Texture2D tex = new Texture2D(imageWidth, imageHeight);
        tex.SetPixels(mapColor);
        tex.Apply();
        SaveTextureToFile(tex, "depthMapTexture");
        GenerateHeightMap(ref tex);
    }

    public void GenerateHeightMap(ref Texture2D tex)
    {
        Vector3 size = new Vector3(200, 30, 200);
        gameObject.AddComponent<MeshFilter>();
        gameObject.AddComponent<MeshRenderer>();
         if (mapMaterial)
             renderer.material = mapMaterial;
         else
             renderer.material.color = Color.white;

        Mesh mesh = GetComponent<MeshFilter>().mesh;

        int width = Mathf.Min(tex.width, 255);
        int height = Mathf.Min(tex.height, 255);
        int x = 0;
        int y = 0;

        //Build vertices and UVs
        Vector3[] vertices = new Vector3[height * width]; //高度图的每一个像素对应一个顶点
        Vector2[] uv = new Vector2[height * width];       //相应的每一个顶点要一个UV和一个切线
        Vector4[] tangents = new Vector4[height * width];

        Vector2 uvScale = new Vector2(1.0f / (width - 1), 1.0f / (height - 1));
        Vector3 sizeScale = new Vector3(size.x / (width - 1), size.y, size.z / (height - 1));


        for (y = 0; y < height; y++)
        {
            for (x = 0; x < width; x++)
            {
                var pixelHeight = tex.GetPixel(x, y).grayscale; //读取每一个像素的灰度值
                var vertex = new Vector3(x, pixelHeight, y);
                vertices[y * width + x] = Vector3.Scale(sizeScale, vertex);
                uv[y * width + x] = Vector2.Scale(new Vector2(x, y), uvScale);

                // Calculate tangent vector: a vector that goes from previous vertex
                // to next along X direction. We need tangents if we intend to
                // use bumpmap shaders on the mesh.
                var vertexL = new Vector3(x - 1, tex.GetPixel(x - 1, y).grayscale, y);
                var vertexR = new Vector3(x + 1, tex.GetPixel(x + 1, y).grayscale, y);
                var tan = Vector3.Scale(sizeScale, vertexR - vertexL).normalized;
                tangents[y * width + x] = new Vector4(tan.x, tan.y, tan.z, -1.0f);//计算切线的方法
            }
        }

        // Assign them to the mesh
        mesh.vertices = vertices;
        mesh.uv = uv;

        // Build triangle indices: 3 indices into vertex array for each triangle
        var triangles = new int[(height - 1) * (width - 1) * 6];
        var index = 0;
        for (y = 0; y < height - 1; y++)
        {
            for (x = 0; x < width - 1; x++)
            {
                // For each grid cell output two triangles
                triangles[index++] = (y * width) + x;
                triangles[index++] = ((y + 1) * width) + x;
                triangles[index++] = (y * width) + x + 1;

                triangles[index++] = ((y + 1) * width) + x;
                triangles[index++] = ((y + 1) * width) + x + 1;
                triangles[index++] = (y * width) + x + 1;
            }
        }
        // And assign them to the mesh
        mesh.triangles = triangles;

        // Auto-calculate vertex normals from the mesh
        mesh.RecalculateNormals();

        // Assign tangents after recalculating normals
        mesh.tangents = tangents;
    }

    void SaveTextureToFile(Texture2D tex, string fileName)
    {
        var bytes = tex.EncodeToPNG();
        var file = File.Open(Application.dataPath + "/" + fileName+".PNG", FileMode.Create);
        var binary = new BinaryWriter(file);
        binary.Write(bytes);
        file.Close();
    }
    void OnApplicationQuit()
    {
        KinectWrapper.NuiShutdown();
    }
}
