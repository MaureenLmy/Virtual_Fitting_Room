using UnityEngine;
using System.Collections;

public class WebCamImage : MonoBehaviour {
    //���ｫ����������ͷ����Kinect�Ĳ�ͼ����ͷ
	//���ⲿ����ͷ��һ����ȱ�ݣ� û�취�����ͼ�͹���ͼ��Ӧ����

    private WebCamTexture tex;

    void Start()
    {
        StartCoroutine(StartCamera());
    }
	// Update is called once per frame
	void Update () 
    {
        renderer.material.mainTexture = tex;
	}

    IEnumerator StartCamera()
    {
        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);//��Ȩ

        if (Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            WebCamDevice[] devices = WebCamTexture.devices;
            string deviceName = devices[0].name;
            //������������������
            tex = new WebCamTexture(deviceName, 640, 480, 30);//set???xyz
            tex.Play();//��ʼ����
        }
    }
}
