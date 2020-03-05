using UnityEngine;
using System.Collections;

public class WebCamImage : MonoBehaviour {
    //这里将用其它摄像头代替Kinect的彩图摄像头
	//用外部摄像头有一个大缺陷， 没办法和深度图和骨骼图对应起来

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
        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);//授权

        if (Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            WebCamDevice[] devices = WebCamTexture.devices;
            string deviceName = devices[0].name;
            //设置摄像机摄像的区域
            tex = new WebCamTexture(deviceName, 640, 480, 30);//set???xyz
            tex.Play();//开始摄像
        }
    }
}
