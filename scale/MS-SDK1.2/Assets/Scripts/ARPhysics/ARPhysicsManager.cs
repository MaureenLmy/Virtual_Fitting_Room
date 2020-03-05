using UnityEngine;
using System.Collections;
using System;

public class ARPhysicsManager : MonoBehaviour {

    #region vars
    //RGBImage ����
    private IntPtr colorStreamHandle;// Image stream handles for the kinect
    private int imageWidth, imageHeight;
    private Color32[] colorImage;    // Color image data, if used
    private Texture2D colorMap;      //����������ʾ�Ĳ�ɫͼ

    //Skeleton ����
    private KinectWrapper.NuiSkeletonFrame skeletonFrame;
    private KinectWrapper.NuiTransformSmoothParameters smoothParameters;
    public Vector3 rightHandScreenPos = Vector3.zero; //��ȱ�ֵ���Ļ����

    //Colliders ��ײ��
    public Transform rightHandCollider;
    public Transform leftHandCollider;
    public Transform leftFootCollider;
    public Transform rightFootCollider;

    public Renderer RGBRenderer; //������ʾRGBͼƬ��renderer
    #endregion vars

    void Start()//??kinect??????
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


            hr = KinectWrapper.NuiSkeletonTrackingEnable(IntPtr.Zero, 8);  // 0, 12,8
            if (hr != 0)
            {
                throw new Exception("Cannot initialize Skeleton Data");
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
        RGBRenderer.material.mainTexture = colorMap;


        // init skeleton structures
        skeletonFrame = new KinectWrapper.NuiSkeletonFrame();

    }

    // Update is called once per frame
    void Update()
    {
        #region Handle ColorFrame
        if (colorStreamHandle != IntPtr.Zero && KinectWrapper.PollColor(colorStreamHandle, ref colorImage))//�ڶ����������������ոո��ºõ���ɫͼ��Color32����
        {
            UpdateColorMap();
        }
        #endregion Handle ColorFrame

        #region Handle SkeletonFrame
        if (KinectWrapper.PollSkeleton(ref smoothParameters, ref skeletonFrame))
        {
            KinectWrapper.NuiSkeletonData skeleton = GetClosestSkeleton();

            if (skeleton.eTrackingState != KinectWrapper.NuiSkeletonTrackingState.NotTracked)
            {
                //������Ҫ�ڸı�ػ���ײ�е�λ��
                SetColliderPosition(ref skeleton, KinectWrapper.NuiSkeletonPositionIndex.HandRight, ref rightHandCollider);
                SetColliderPosition(ref skeleton, KinectWrapper.NuiSkeletonPositionIndex.HandLeft, ref leftHandCollider);
                SetColliderPosition(ref skeleton, KinectWrapper.NuiSkeletonPositionIndex.FootRight, ref rightFootCollider);
                SetColliderPosition(ref skeleton, KinectWrapper.NuiSkeletonPositionIndex.FootLeft, ref leftFootCollider);
            }
        }
        #endregion HandleSkeleton Frame

        if (Input.GetKey(KeyCode.Escape)) Application.Quit();
    }

    // Update the Color Map
	void UpdateColorMap()//???????????????
    {
        colorMap.SetPixels32(colorImage);
        colorMap.Apply();     
    }


    void SetColliderPosition( ref KinectWrapper.NuiSkeletonData skeleton, KinectWrapper.NuiSkeletonPositionIndex index,
                              ref Transform jointCollider) //���ݹ�����λ����������ײ�е�λ��
    {
        if (skeleton.eSkeletonPositionTrackingState[(int)index] == KinectWrapper.NuiSkeletonPositionTrackingState.Tracked)
        {
            Vector3 Pos = skeleton.SkeletonPositions[(int)index];
            Vector3 ScreenPos = KinectWrapper.MapSkeletonPointToColorPoint(Pos);
            //ע�⣬���ﷵ�ص���Ļ�������ڣ�640x480�ֱ����²Ŷ�Ӧ���ϵģ������ֱ������ٳ���һ�����������Ϳ����� 
            ScreenPos.x *= Screen.width / (float)KinectWrapper.GetDepthWidth();
            ScreenPos.y *= Screen.height / (float)KinectWrapper.GetDepthHeight();

            Vector3 worldPos=Camera.main.ScreenToWorldPoint(new Vector3( ScreenPos.x,Screen.height-ScreenPos.y,RGBRenderer.transform.position.z)); //Ҫ��RGBͼ��Zֵ����ȥ����
            jointCollider.position = new Vector3(worldPos.x, worldPos.y, jointCollider.position.z); //ֻ�ı�x y ����ֵ
        }
        
    }
   
    
    //�򵥵ø��ݹ���λ�õ�zֵ���������һ����������
    KinectWrapper.NuiSkeletonData GetClosestSkeleton()//?????????????
    {
        KinectWrapper.NuiSkeletonData skeletonData = new KinectWrapper.NuiSkeletonData();
        float minDistance = 8f;
        for (int i = 0; i < KinectWrapper.Constants.NuiSkeletonCount; i++)
        {
            KinectWrapper.NuiSkeletonData skeleton = skeletonFrame.SkeletonData[i];
            if (skeleton.eTrackingState != KinectWrapper.NuiSkeletonTrackingState.NotTracked)
            {
                if (skeleton.Position.z < minDistance)
                {
                    skeletonData = skeleton;
                    minDistance = skeleton.Position.z;
                }
            }
        }

        return skeletonData;
    }

    
    void OnApplicationQuit() //�������Ҫ��Щ��β��������ȻKinect�����Զ��ر�
    {
        KinectWrapper.NuiShutdown();
    }
}
