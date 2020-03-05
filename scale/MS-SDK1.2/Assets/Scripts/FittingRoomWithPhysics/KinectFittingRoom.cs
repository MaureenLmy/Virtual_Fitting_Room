using UnityEngine;
using System.Collections;
using System;

public class KinectFittingRoom : MonoBehaviour {

    #region vars
    //RGBImage ����
    private IntPtr colorStreamHandle;// Image stream handles for the kinect
    private int imageWidth, imageHeight;
    private Color32[] colorImage;    // Color image data, if used
    private Texture2D colorMap;      //����������ʾ�Ĳ�ɫͼ

    //Skeleton ����
    private KinectWrapper.NuiSkeletonFrame skeletonFrame;
    private KinectWrapper.NuiTransformSmoothParameters smoothParameters;


    public Renderer RGBRenderer;    //������ʾRGBͼ�������
    public Transform shirtTransform;
    public Transform shortPantTransform;
    public Transform bagTransform;

    public Transform shoulderLCollider;
    public Transform shoulderRCollider;
    public Transform rightArmCollider;

    public Transform hipRightCollider;
    public Transform hipLeftCollider;
    public Transform handRightCollider;
    public Transform handLeftCollider;
    public Transform kneeRightCollider;
    public Transform kneeLeftCollider;
    public Transform bodyCollider; //������ס�·�ʹ�䲻����Ʈ�����


    public float lerpSpeed = 3; //���л��ƹؽڵ��ٶ�

    public ClothRenderer pantRenderer;
    public ClothRenderer shirtRenderer;
    public Texture2D[] pantTexs;
    public Texture2D[] shirtTexs;
    private int pantIndex=0;
    private int shirtIndex=0;
    private float lastChangeTime=0;

    public float zOffsetFromRGBScreen = 5; //ȫ���ؽڶ����RGB��Ļ��Z��ƫ�������
    #endregion vars

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

        //show the RGB image
        RGBRenderer.material.mainTexture = colorMap;

        // init skeleton structures
        skeletonFrame = new KinectWrapper.NuiSkeletonFrame();


        bodyCollider.position = new Vector3(bodyCollider.position.x, bodyCollider.position.y, RGBRenderer.transform.position.z - zOffsetFromRGBScreen);
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
                Vector3 leftHandPos = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.HandLeft];
                Vector3 rightHandPos = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.HandRight];
                Vector3 leftShoulderPos = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.ShoulderLeft];
                Vector3 rightShoulderPos = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.ShoulderRight];
                if (leftHandPos.y > leftShoulderPos.y)
                    ChangePant();
                if (rightHandPos.y > rightShoulderPos.y)
                    ChangeShirt();

                SetLeftShoulderTransform(ref skeleton, ref shoulderLCollider);
                SetRightShoulderTransform(ref skeleton, ref shoulderRCollider);
                //SetRightArmTransform(ref skeleton, ref rightArmCollider);
                SetRightHipTransform(ref skeleton, ref hipRightCollider);
                SetLeftHipTransform(ref skeleton, ref hipLeftCollider);
                SetRightHandTransform(ref skeleton, ref handRightCollider);
                SetRightKneeTransform(ref skeleton, ref kneeRightCollider);
                SetLeftKneeTransform(ref skeleton, ref kneeLeftCollider);

            }
        }
        #endregion HandleSkeleton Frame

        if (Input.GetKey(KeyCode.Escape)) Application.Quit();
    }


    void SetColliderPosition(ref KinectWrapper.NuiSkeletonData skeleton, KinectWrapper.NuiSkeletonPositionIndex index,
                              ref Transform jointCollider) //���ݹ�����λ����������ײ�е�λ��
    {
        if (skeleton.eSkeletonPositionTrackingState[(int)index] == KinectWrapper.NuiSkeletonPositionTrackingState.Tracked)
        {
            Vector3 Pos = skeleton.SkeletonPositions[(int)index];
            Vector3 ScreenPos = KinectWrapper.MapSkeletonPointToColorPoint(Pos);
            //ע�⣬���ﷵ�ص���Ļ�������ڣ�640x480�ֱ����²Ŷ�Ӧ���ϵģ������ֱ������ٳ���һ�����������Ϳ����� 
            ScreenPos.x *= Screen.width / (float)KinectWrapper.GetDepthWidth();
            ScreenPos.y *= Screen.height / (float)KinectWrapper.GetDepthHeight();

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(ScreenPos.x, Screen.height - ScreenPos.y, RGBRenderer.transform.position.z)); //Ҫ��RGBͼ��Zֵ����ȥ����
            jointCollider.position = new Vector3(worldPos.x, worldPos.y, jointCollider.position.z); //ֻ�ı�x y ����ֵ
        }

    }
    // Update the Color Map
    void UpdateColorMap()
    {
        colorMap.SetPixels32(colorImage);
        colorMap.Apply();
    }



    #region �����·���Ƭλ�úͽǶȵĸ��ֺ����� �����·��Ĳ�ͬ��λ�����ݵĹؽڲ�һ��������Ҫ�в�ͬ�ĺ�������
    void SetLeftShoulderTransform(ref KinectWrapper.NuiSkeletonData skeleton, ref Transform jointCollider) //���ݹ�����λ����������ײ�е�λ��
    {
        if (skeleton.eSkeletonPositionTrackingState[(int)KinectWrapper.NuiSkeletonPositionIndex.ShoulderLeft] != KinectWrapper.NuiSkeletonPositionTrackingState.NotTracked)
            
        {
            //�������Ǹ���shoulderCenter��������Ƭ��λ�ã� �ٸ���spine��shoulderCenter���ɵ�ʸ��ƫ�봹ֱ����ļн���������Ƭ��ŷ����
            Vector3 shouderLeftPos = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.ShoulderLeft];
            
            Vector3 shoulderLeftScreenPos = GetJointInScreenPosition(shouderLeftPos);

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(shoulderLeftScreenPos.x, Screen.height - shoulderLeftScreenPos.y, RGBRenderer.transform.position.z)); //Ҫ��RGBͼ��Zֵ����ȥ����
            float zOffSet = shouderLeftPos.z - skeleton.Position.z; 
            //����ֱ����Kienct���ص�Zֵ�������Ȼ���ܺͺ����漸��ת���õ����������������Ӧ�ϣ��������������һ������ϵ������
            zOffSet *= 15;
            Vector3 targetPos = new Vector3(worldPos.x, worldPos.y + 2.5f, RGBRenderer.transform.position.z + zOffSet - zOffsetFromRGBScreen); //ֻ�ı�x y ����ֵ
            jointCollider.position = Vector3.Lerp(jointCollider.position, targetPos, Time.deltaTime*lerpSpeed); 
           
        }
    }

    void SetRightShoulderTransform(ref KinectWrapper.NuiSkeletonData skeleton, ref Transform jointCollider) //���ݹ�����λ����������ײ�е�λ��
    {
        if (skeleton.eSkeletonPositionTrackingState[(int)KinectWrapper.NuiSkeletonPositionIndex.ShoulderRight] != KinectWrapper.NuiSkeletonPositionTrackingState.NotTracked)
        {
           Vector3 shouderRightPos = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.ShoulderRight];

            Vector3 shoulderRightScreenPos = GetJointInScreenPosition(shouderRightPos);

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(shoulderRightScreenPos.x, Screen.height - shoulderRightScreenPos.y, RGBRenderer.transform.position.z)); //Ҫ��RGBͼ��Zֵ����ȥ����
            float zOffset = shouderRightPos.z - skeleton.Position.z;
            //����ֱ����Kienct���ص�Zֵ�������Ȼ���ܺͺ����漸��ת���õ����������������Ӧ�ϣ��������������һ������ϵ������
            zOffset *= 15;
            
             Vector3 targetPos= new Vector3(worldPos.x, worldPos.y+2.5f, RGBRenderer.transform.position.z+zOffset-zOffsetFromRGBScreen); //ֻ�ı�x y ����ֵ
             jointCollider.position = Vector3.Lerp(jointCollider.position, targetPos, Time.deltaTime*lerpSpeed);                                             //2.5��Ϊ�˲���collider�ڵ����·�����ƫ��                   //��5����Ϊ���ÿ��Ƶ㲻��������ʾ��

        }
    }

    void SetRightHipTransform(ref KinectWrapper.NuiSkeletonData skeleton, ref Transform jointCollider) //���ݹ�����λ����������ײ�е�λ��
    {
        if (skeleton.eSkeletonPositionTrackingState[(int)KinectWrapper.NuiSkeletonPositionIndex.HipRight] != KinectWrapper.NuiSkeletonPositionTrackingState.NotTracked)
        {
            Vector3 hipRightPos = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.HipRight];

            Vector3 hipRightScreenPos = GetJointInScreenPosition(hipRightPos);

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(hipRightScreenPos.x, Screen.height - hipRightScreenPos.y, RGBRenderer.transform.position.z)); //Ҫ��RGBͼ��Zֵ����ȥ����
            float zOffset = hipRightPos.z - skeleton.Position.z;
            Vector3 targetPos = new Vector3(worldPos.x + 1.5f, worldPos.y, RGBRenderer.transform.position.z + zOffset-zOffsetFromRGBScreen); //ֻ�ı�x y ����ֵ
            jointCollider.position = Vector3.Lerp(jointCollider.position, targetPos, Time.deltaTime * lerpSpeed);

        }
    }

    void SetLeftHipTransform(ref KinectWrapper.NuiSkeletonData skeleton, ref Transform jointCollider) //���ݹ�����λ����������ײ�е�λ��
    {
        if (skeleton.eSkeletonPositionTrackingState[(int)KinectWrapper.NuiSkeletonPositionIndex.HipLeft] != KinectWrapper.NuiSkeletonPositionTrackingState.NotTracked)
        {
            Vector3 hipLeftPos = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.HipLeft];

            Vector3 hipLeftScreenPos = GetJointInScreenPosition(hipLeftPos);

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(hipLeftScreenPos.x, Screen.height - hipLeftScreenPos.y, RGBRenderer.transform.position.z)); //Ҫ��RGBͼ��Zֵ����ȥ����
            float zOffset = hipLeftPos.z - skeleton.Position.z;
            Vector3 targetPos = new Vector3(worldPos.x - 1.5f, worldPos.y, RGBRenderer.transform.position.z + zOffset-zOffsetFromRGBScreen); //ֻ�ı�x y ����ֵ
                                                        //1.5f ��Ϊ��ʵ���β������Ե���ιؽڵ�ƫ��
            jointCollider.position = Vector3.Lerp(jointCollider.position, targetPos, Time.deltaTime * lerpSpeed);

        }
    }

    void SetRightKneeTransform(ref KinectWrapper.NuiSkeletonData skeleton, ref Transform jointCollider) //���ݹ�����λ����������ײ�е�λ��
    {
        if (skeleton.eSkeletonPositionTrackingState[(int)KinectWrapper.NuiSkeletonPositionIndex.KneeRight] != KinectWrapper.NuiSkeletonPositionTrackingState.NotTracked)
        {
            Vector3 kneeRightPos = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.KneeRight];

            Vector3 kneeRightScreenPos = GetJointInScreenPosition(kneeRightPos);

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(kneeRightScreenPos.x, Screen.height - kneeRightScreenPos.y, RGBRenderer.transform.position.z)); //Ҫ��RGBͼ��Zֵ����ȥ����
            float zOffset = kneeRightPos.z - skeleton.Position.z;
            zOffset *= 20;
            jointCollider.position = new Vector3(worldPos.x, worldPos.y, RGBRenderer.transform.position.z + zOffset - zOffsetFromRGBScreen); //ֻ�ı�x y ����ֵ

        }
    }

    void SetLeftKneeTransform(ref KinectWrapper.NuiSkeletonData skeleton, ref Transform jointCollider) //���ݹ�����λ����������ײ�е�λ��
    {
        if (skeleton.eSkeletonPositionTrackingState[(int)KinectWrapper.NuiSkeletonPositionIndex.KneeLeft] != KinectWrapper.NuiSkeletonPositionTrackingState.NotTracked)
        {
            Vector3 kneeLeftPos = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.KneeLeft];

            Vector3 kneeLeftScreenPos = GetJointInScreenPosition(kneeLeftPos);

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(kneeLeftScreenPos.x, Screen.height - kneeLeftScreenPos.y, RGBRenderer.transform.position.z)); //Ҫ��RGBͼ��Zֵ����ȥ����
            float zOffset = kneeLeftPos.z - skeleton.Position.z;
            zOffset *= 20;
            jointCollider.position = new Vector3(worldPos.x, worldPos.y, RGBRenderer.transform.position.z + zOffset - zOffsetFromRGBScreen); //ֻ�ı�x y ����ֵ

        }
    }
    void SetRightHandTransform(ref KinectWrapper.NuiSkeletonData skeleton, ref Transform jointCollider) //���ݹ�����λ����������ײ�е�λ��
    {
        if (skeleton.eSkeletonPositionTrackingState[(int)KinectWrapper.NuiSkeletonPositionIndex.HandRight] != KinectWrapper.NuiSkeletonPositionTrackingState.NotTracked)
        {
            Vector3 handRightPos = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.HandRight];

            Vector3 handRightScreenPos = GetJointInScreenPosition(handRightPos);

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(handRightScreenPos.x, Screen.height - handRightScreenPos.y, RGBRenderer.transform.position.z)); //Ҫ��RGBͼ��Zֵ����ȥ����
            float zOffset = handRightPos.z - skeleton.Position.z;
            jointCollider.position = new Vector3(worldPos.x + 1.5f, worldPos.y, RGBRenderer.transform.position.z - zOffset-zOffsetFromRGBScreen-zOffsetFromRGBScreen); //ֻ�ı�x y ����ֵ

        }
    }
    void SetRightArmTransform(ref KinectWrapper.NuiSkeletonData skeleton, ref Transform armCollider)
    {
         //�������Ǹ���shoulderCenter��������Ƭ��λ�ã� �ٸ���spine��shoulderCenter���ɵ�ʸ��ƫ�봹ֱ����ļн���������Ƭ��ŷ����
            Vector3 shouderRightPos = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.ShoulderRight];

            Vector3 shoulderRightScreenPos = GetJointInScreenPosition(shouderRightPos);

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(shoulderRightScreenPos.x, Screen.height - shoulderRightScreenPos.y, RGBRenderer.transform.position.z)); //Ҫ��RGBͼ��Zֵ����ȥ����
            float zOffset = shouderRightPos.z - skeleton.Position.z;
            armCollider.position = new Vector3(worldPos.x, worldPos.y, RGBRenderer.transform.position.z+zOffset); //ֻ�ı�x y ����ֵ

    }
    
   

    
    
    #endregion �����·���Ƭ����

    #region ��������
    Vector3 GetJointInScreenPosition(Vector3 jointPos)   //��������� KinectWrapper.MapSkeletonPointToColorPoint() �Ļ����ϼ�����Ļ�ֱ�������Ӧ
    {
        Vector3 ScreenPos = KinectWrapper.MapSkeletonPointToColorPoint(jointPos);
        ScreenPos.x *= Screen.width / (float)KinectWrapper.GetDepthWidth();  //�Զ���Ӧ�ֱ���
        ScreenPos.y *= Screen.height / (float)KinectWrapper.GetDepthHeight();

        return ScreenPos;
    }

    void ChangeShirt()
    {
        if (Time.realtimeSinceStartup - lastChangeTime > 2f)
        {
            shirtIndex++;
            if (shirtIndex >= shirtTexs.Length) shirtIndex = 0;
            shirtRenderer.material.mainTexture = shirtTexs[shirtIndex];
            lastChangeTime = Time.realtimeSinceStartup;
        }

    }

    void ChangePant()
    {
        if (Time.realtimeSinceStartup - lastChangeTime > 2f)
        {
            pantIndex++;
            if (pantIndex >= pantTexs.Length) pantIndex = 0;
            pantRenderer.material.mainTexture = pantTexs[pantIndex];
            lastChangeTime = Time.realtimeSinceStartup;
        }

    }
    KinectWrapper.NuiSkeletonData GetClosestSkeleton()
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
    #endregion ��������


    void OnApplicationQuit() //�������Ҫ��Щ��β��������ȻKinect�����Զ��ر�
    {
        KinectWrapper.NuiShutdown();
    }
}
