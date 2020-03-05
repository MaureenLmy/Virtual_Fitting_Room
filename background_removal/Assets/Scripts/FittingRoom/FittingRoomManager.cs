using UnityEngine;
using System.Collections;
using System;

public class FittingRoomManager : MonoBehaviour
{
    #region vars
    //RGBImage ����
    private IntPtr colorStreamHandle;// Image stream handles for the kinect
    private int imageWidth, imageHeight;
    private Color32[] colorImage;    // Color image data, if used
    private Texture2D colorMap;      //����������ʾ�Ĳ�ɫͼ
	public bool useMirrorImage = true;

	public bool NearMode = true;
	private IntPtr depthStreamHandle;
	private short[] usersDepthMap;
	public int pixelOffset;
	
	//y������ʾ�ĵط�
	public Renderer colorImageRenderer;
	public Renderer depthImageRenderer;

    //Skeleton ����
    private KinectWrapper.NuiSkeletonFrame skeletonFrame;
    private KinectWrapper.NuiTransformSmoothParameters smoothParameters;
    

    public Renderer RGBRenderer;    //������ʾRGBͼ�������
    public Transform shirtTransform;
    public Transform shortPantTransform;
    public Transform bagTransform;
	public Transform hatTransform;
	public Transform scarpTransform;

    //�������б������ŵı���
    public Transform shoulderLeft;
    public Transform shoulderRight;
    private float originalDistance;
    private Vector3 originalScale;


    public ClothesManager clothesManger;
	public background Background;
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

			depthStreamHandle = IntPtr.Zero;
			hr = KinectWrapper.NuiImageStreamOpen(KinectWrapper.NuiImageType.DepthAndPlayerIndex,
			                                      KinectWrapper.Constants.ImageResolution, 0, 2, IntPtr.Zero, ref depthStreamHandle);
			if (hr != 0)
			{
				throw new Exception("Cannot open depth stream");
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

		usersDepthMap = new short[usersMapSize];
		pixelOffset = imageWidth * 20;
        
        //show the RGB image
        RGBRenderer.material.mainTexture = colorMap;

        // init skeleton structures
        skeletonFrame = new KinectWrapper.NuiSkeletonFrame();

        clothesManger = GetComponent<ClothesManager>();
		Background = GetComponent<background>();

        originalDistance = Vector3.Distance(shoulderLeft.position, shoulderRight.position); //��ʼ��ʱ���������Ƕ�����������ľ���
        originalScale = shirtTransform.localScale;                                          //��ʼʱ��������Ƭ��scale����
        
    }

    // Update is called once per frame
    void Update()
    {
		if (depthStreamHandle != IntPtr.Zero && KinectWrapper.PollDepth(depthStreamHandle, NearMode, ref usersDepthMap))
		{
		}
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
                if (clothesManger != null) //������һ���ű��������·���ѡ��ȵ�
                {
                    if (skeleton.eSkeletonPositionTrackingState[(int)KinectWrapper.NuiSkeletonPositionIndex.HandRight] != KinectWrapper.NuiSkeletonPositionTrackingState.NotTracked)
                    {
                        Vector3 rightHandPos = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.HandRight];
                        clothesManger.HandPositionUpdate(rightHandPos);
                    }
                }
				if (Background != null) //������һ���ű��������·���ѡ��ȵ�
				{
					if (skeleton.eSkeletonPositionTrackingState[(int)KinectWrapper.NuiSkeletonPositionIndex.HandLeft] != KinectWrapper.NuiSkeletonPositionTrackingState.NotTracked)
					{
						Vector3 leftHandPos = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.HandLeft];
						Background.HandPositionUpdate(leftHandPos);
					}
				}
                SetTShirtTransform(ref skeleton, ref shirtTransform);
                SetShortPantTransform(ref skeleton, ref shortPantTransform);
                SetBagTransform(ref skeleton, ref bagTransform);
				SetHatTransform(ref skeleton, ref hatTransform);
				SetScarpTransform(ref skeleton, ref scarpTransform);
				ResetShirtScale(ref skeleton);
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
				//colorImage[flipIndex] = Background.getpixel(i);
				//colorImage[i] = Color.clear;
			}	
		}
        colorMap.SetPixels32(colorImage);
        colorMap.Apply();    
    }

	int ReverseMapIndex(int index, int width, int height)
	{
		int reverseIndex=(width*width-(index/width)*width+height-index%width);
		//C# �����û���������������������ĳ��ž͵���������������
		return reverseIndex-1;
		
	}

    #region �����·���Ƭλ�úͽǶȵĸ��ֺ����� �����·��Ĳ�ͬ��λ�����ݵĹؽڲ�һ��������Ҫ�в�ͬ�ĺ�������

	void SetHatTransform(ref KinectWrapper.NuiSkeletonData skeleton, ref Transform jointTransform)
	{
		if (skeleton.eSkeletonPositionTrackingState[(int)KinectWrapper.NuiSkeletonPositionIndex	.Head] != KinectWrapper.NuiSkeletonPositionTrackingState.NotTracked)
		{
			Vector3 HeadPos = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.Head];
			Vector3 HeadScreenPos = GetJointInScreenPosition(HeadPos);
			
			Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(HeadScreenPos.x, Screen.height - HeadScreenPos.y, RGBRenderer.transform.position.z)); //??RGB??Z??????
			jointTransform.position = new Vector3(worldPos.x, worldPos.y, jointTransform.position.z); //???x y ???
			
		}
	}
	void SetScarpTransform(ref KinectWrapper.NuiSkeletonData skeleton, ref Transform jointTransform)
	{
		if (skeleton.eSkeletonPositionTrackingState[(int)KinectWrapper.NuiSkeletonPositionIndex	.Head] != 
		    KinectWrapper.NuiSkeletonPositionTrackingState.NotTracked)
		{
			Vector3 ShoulderCenterPos = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.ShoulderCenter];
			Vector3 ShoulderCenterScreenPos = GetJointInScreenPosition(ShoulderCenterPos);
			
			Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(ShoulderCenterScreenPos.x, Screen.height - ShoulderCenterScreenPos.y
			                                                              , RGBRenderer.transform.position.z)); //??RGB??Z??????
			jointTransform.position = new Vector3(worldPos.x, worldPos.y, jointTransform.position.z); //???x y ???
			
		}
	}
	void SetTShirtTransform(ref KinectWrapper.NuiSkeletonData skeleton,ref Transform jointCollider) //���ݹ�����λ����������ײ�е�λ��
    {
        if (skeleton.eSkeletonPositionTrackingState[(int)KinectWrapper.NuiSkeletonPositionIndex.Spine] != KinectWrapper.NuiSkeletonPositionTrackingState.NotTracked
            && skeleton.eSkeletonPositionTrackingState[(int)KinectWrapper.NuiSkeletonPositionIndex.ShoulderCenter] != KinectWrapper.NuiSkeletonPositionTrackingState.NotTracked)
        {
            //�������Ǹ���shoulderCenter��������Ƭ��λ�ã� �ٸ���spine��shoulderCenter���ɵ�ʸ��ƫ�봹ֱ����ļн���������Ƭ��ŷ����
            Vector3 shouderCenterPos = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.ShoulderCenter];
            Vector3 SpinePos = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.Spine];
            Vector3 shoulderCenterScreenPos = GetJointInScreenPosition(shouderCenterPos);

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(shoulderCenterScreenPos.x, Screen.height - shoulderCenterScreenPos.y, RGBRenderer.transform.position.z)); //Ҫ��RGBͼ��Zֵ����ȥ����
            jointCollider.position = new Vector3(worldPos.x, worldPos.y, jointCollider.position.z); //ֻ�ı�x y ����ֵ

            //����������λ�ã�����������λ��
            Vector3 upDirection = shouderCenterPos - SpinePos;
            jointCollider.up = upDirection; //ͬʱ��up ��right �Ļ���ʹ�������X�����ת��Ч������Y����ת��ȣ�Ŀǰֻ��������X��

            Vector3 rightDirection = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.ShoulderRight] - skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.ShoulderLeft];
            jointCollider.right = rightDirection; 

            //Ҫ���õ�Ч��Ҫ���ŷֱ���ŷ����
        }

    }

    void SetShortPantTransform(ref KinectWrapper.NuiSkeletonData skeleton, ref Transform jointTransform)
    {
        if (skeleton.eSkeletonPositionTrackingState[(int)KinectWrapper.NuiSkeletonPositionIndex.HipCenter] != KinectWrapper.NuiSkeletonPositionTrackingState.NotTracked)
        {
            Vector3 hipCenterPos = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.HipCenter];
            Vector3 hipScreenPos = GetJointInScreenPosition(hipCenterPos);

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(hipScreenPos.x, Screen.height - hipScreenPos.y, RGBRenderer.transform.position.z)); //Ҫ��RGBͼ��Zֵ����ȥ����
            jointTransform.position = new Vector3(worldPos.x, worldPos.y, jointTransform.position.z); //ֻ�ı�x y ����ֵ

            Vector3 rightDirection = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.HipRight] - skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.HipLeft];
            jointTransform.right = rightDirection; 

        }
    }


    void SetBagTransform(ref KinectWrapper.NuiSkeletonData skeleton, ref Transform jointTransform)
    {
        if (skeleton.eSkeletonPositionTrackingState[(int)KinectWrapper.NuiSkeletonPositionIndex.HandLeft] != KinectWrapper.NuiSkeletonPositionTrackingState.NotTracked)
        {
            Vector3 handLeftPos = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.HandLeft];
            Vector3 handLeftScreenPos = GetJointInScreenPosition(handLeftPos);

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(handLeftScreenPos.x, Screen.height - handLeftScreenPos.y, RGBRenderer.transform.position.z)); //Ҫ��RGBͼ��Zֵ����ȥ����
            jointTransform.position = new Vector3(worldPos.x, worldPos.y, jointTransform.position.z); //ֻ�ı�x y ����ֵ

        }
    }

    void ResetShirtScale(ref KinectWrapper.NuiSkeletonData skeleton)
    {
        if (skeleton.eSkeletonPositionTrackingState[(int)KinectWrapper.NuiSkeletonPositionIndex.ShoulderLeft] != KinectWrapper.NuiSkeletonPositionTrackingState.NotTracked
           && skeleton.eSkeletonPositionTrackingState[(int)KinectWrapper.NuiSkeletonPositionIndex.ShoulderRight] != KinectWrapper.NuiSkeletonPositionTrackingState.NotTracked)
        {
            //�������Ǹ���shoulderCenter��������Ƭ��λ�ã� �ٸ���spine��shoulderCenter���ɵ�ʸ��ƫ�봹ֱ����ļн���������Ƭ��ŷ����
            Vector3 shouderLeftPos = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.ShoulderLeft];
            Vector3 shoulderRightPos = skeleton.SkeletonPositions[(int)KinectWrapper.NuiSkeletonPositionIndex.ShoulderRight];
            Vector3 shoulderLeftScreenPos = GetJointInScreenPosition(shouderLeftPos);
            Vector3 shoulderRightScreenPos = GetJointInScreenPosition(shoulderRightPos);

            Vector3 shouderLWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(shoulderLeftScreenPos.x, Screen.height - shoulderLeftScreenPos.y, RGBRenderer.transform.position.z)); //Ҫ��RGBͼ��Zֵ����ȥ����
            Vector3 shouderRWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(shoulderRightScreenPos.x, Screen.height - shoulderRightScreenPos.y, RGBRenderer.transform.position.z)); //Ҫ��RGBͼ��Zֵ����ȥ����

            float curDistance = Vector3.Distance(shouderLWorldPos, shouderRWorldPos);
            float scaleFactor = curDistance / originalDistance;
            shirtTransform.localScale = originalScale * scaleFactor;
           
        }
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